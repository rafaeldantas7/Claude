#!/usr/bin/env python3
"""
Interface Streamlit para o Agente de Tradução — Literatura Cristã Reformada.

Execute com:
    streamlit run app.py
"""

import io
import tempfile
from pathlib import Path

import streamlit as st

# ---------------------------------------------------------------------------
# Configuração da página
# ---------------------------------------------------------------------------
st.set_page_config(
    page_title="Tradutor — Literatura Cristã Reformada",
    page_icon="✝️",
    layout="wide",
    initial_sidebar_state="collapsed",
)

# ---------------------------------------------------------------------------
# CSS personalizado — estética de pergaminho
# ---------------------------------------------------------------------------
st.markdown(
    """
    <style>
    /* Fundo geral */
    .stApp {
        background-color: #f5f0e8;
        font-family: Georgia, 'Times New Roman', serif;
    }

    /* Cabeçalho principal */
    h1 {
        color: #3b2a1a;
        text-align: center;
        letter-spacing: 0.04em;
    }

    h2, h3 {
        color: #4a3520;
    }

    /* Caixa de tradução */
    .translation-box {
        background-color: #fffdf5;
        border: 1px solid #c8b89a;
        border-radius: 6px;
        padding: 1.4rem 1.8rem;
        font-family: Georgia, serif;
        font-size: 1rem;
        line-height: 1.75;
        color: #2c1e0f;
        white-space: pre-wrap;
        min-height: 200px;
    }

    /* Botões */
    .stButton > button, .stDownloadButton > button {
        background-color: #5c3d1e;
        color: #f5f0e8;
        border: none;
        border-radius: 4px;
        font-family: Georgia, serif;
        font-size: 0.95rem;
        padding: 0.5rem 1.4rem;
    }
    .stButton > button:hover, .stDownloadButton > button:hover {
        background-color: #7a5230;
        color: #fff;
    }

    /* Upload widget */
    .stFileUploader {
        background-color: #fffdf5;
        border: 1px dashed #c8b89a;
        border-radius: 6px;
        padding: 0.6rem;
    }

    /* Barra de progresso */
    .stProgress > div > div > div {
        background-color: #5c3d1e;
    }

    /* Divisores */
    hr {
        border-color: #c8b89a;
    }

    /* Info / warning boxes */
    .stAlert {
        border-radius: 6px;
        font-family: Georgia, serif;
    }
    </style>
    """,
    unsafe_allow_html=True,
)

# ---------------------------------------------------------------------------
# Cabeçalho
# ---------------------------------------------------------------------------
st.markdown("## ✝️ Tradutor de Literatura Cristã Reformada")
st.markdown(
    "_Inglês arcaico (séc. XVI–XIX) → Português · Tradição Reformada e Puritana_"
)
st.divider()

# ---------------------------------------------------------------------------
# Upload
# ---------------------------------------------------------------------------
col_upload, col_info = st.columns([2, 1])

with col_upload:
    uploaded = st.file_uploader(
        "Selecione o arquivo a traduzir",
        type=["txt", "pdf", "docx"],
        help="Suporta .txt, .pdf (texto ou escaneado) e .docx",
    )

with col_info:
    st.markdown(
        """
        **Formatos suportados**
        - `.txt` — texto simples
        - `.pdf` — texto nativo ou escaneado (visão IA)
        - `.docx` — Word

        **Autores suportados**
        Watson · Owen · Edwards · Baxter · Spurgeon · Bunyan · Calvino · e outros
        """
    )

# ---------------------------------------------------------------------------
# Estado da sessão
# ---------------------------------------------------------------------------
if "translation" not in st.session_state:
    st.session_state.translation = ""
if "translating" not in st.session_state:
    st.session_state.translating = False

# ---------------------------------------------------------------------------
# Botão de tradução
# ---------------------------------------------------------------------------
st.divider()
start_col, _ = st.columns([1, 3])
with start_col:
    start = st.button(
        "Traduzir",
        disabled=uploaded is None or st.session_state.translating,
        use_container_width=True,
    )

# ---------------------------------------------------------------------------
# Execução da tradução
# ---------------------------------------------------------------------------
if start and uploaded:
    st.session_state.translation = ""
    st.session_state.translating = True

    from translation_agent import extract_content, split_into_chunks, translate_chunk, translate_image_page, CONTEXT_TAIL
    import anthropic

    file_bytes = uploaded.read()
    filename = uploaded.name

    with tempfile.TemporaryDirectory() as tmp_dir:
        tmp_out = Path(tmp_dir) / "output.txt"

        # Detecta tipo de conteúdo
        content_type, content = extract_content(file_bytes, filename)

        if content_type == "text":
            chunks = split_into_chunks(content)
            total = len(chunks)
        else:
            pages = content
            total = len(pages)

        # Cabeçalho do progresso
        st.markdown(f"**Processando {total} {'trecho(s)' if content_type == 'text' else 'página(s)'}...**")
        progress_bar = st.progress(0.0)
        status_text = st.empty()

        # Container de tradução em tempo real
        st.markdown("### Tradução")
        output_area = st.empty()

        # Buffer para reduzir rerenders
        BUFFER_THRESHOLD = 150
        buffer = ""
        accumulated = ""

        def on_delta(text: str) -> None:
            nonlocal buffer, accumulated
            buffer += text
            accumulated += text
            if len(buffer) >= BUFFER_THRESHOLD:
                st.session_state.translation = accumulated
                output_area.markdown(
                    f'<div class="translation-box">{accumulated}</div>',
                    unsafe_allow_html=True,
                )
                buffer = ""

        def flush_buffer() -> None:
            nonlocal buffer, accumulated
            if buffer:
                st.session_state.translation = accumulated
                output_area.markdown(
                    f'<div class="translation-box">{accumulated}</div>',
                    unsafe_allow_html=True,
                )
                buffer = ""

        client = anthropic.Anthropic()
        translations: list[str] = []
        prev_translation = ""

        if content_type == "text":
            for i, chunk in enumerate(chunks, 1):
                status_text.markdown(f"_Traduzindo trecho {i} de {total}…_")
                translated = translate_chunk(
                    client=client,
                    chunk=chunk,
                    chunk_num=i,
                    total_chunks=total,
                    previous_translation=prev_translation,
                    on_delta=on_delta,
                )
                flush_buffer()
                translations.append(translated)
                prev_translation = translated[-CONTEXT_TAIL:] if len(translated) > CONTEXT_TAIL else translated
                accumulated += "\n\n"
                progress_bar.progress(i / total)
        else:
            for i, page_img in enumerate(pages, 1):
                status_text.markdown(f"_Traduzindo página {i} de {total}…_")
                translated = translate_image_page(
                    client=client,
                    page_image=page_img,
                    page_num=i,
                    total_pages=total,
                    previous_translation=prev_translation,
                    on_delta=on_delta,
                )
                flush_buffer()
                translations.append(translated)
                prev_translation = translated[-CONTEXT_TAIL:] if len(translated) > CONTEXT_TAIL else translated
                accumulated += "\n\n"
                progress_bar.progress(i / total)

        full_translation = "\n\n".join(translations)
        st.session_state.translation = full_translation
        st.session_state.translating = False
        status_text.markdown("**✅ Tradução concluída!**")

# ---------------------------------------------------------------------------
# Exibe tradução armazenada (entre reruns)
# ---------------------------------------------------------------------------
elif st.session_state.translation and not st.session_state.translating:
    st.markdown("### Tradução")
    st.markdown(
        f'<div class="translation-box">{st.session_state.translation}</div>',
        unsafe_allow_html=True,
    )

# ---------------------------------------------------------------------------
# Botão de download
# ---------------------------------------------------------------------------
if st.session_state.translation:
    st.divider()
    stem = Path(uploaded.name).stem if uploaded else "traducao"
    filename_out = f"{stem}_pt.txt"

    st.download_button(
        label="Baixar tradução (.txt)",
        data=st.session_state.translation.encode("utf-8"),
        file_name=filename_out,
        mime="text/plain",
    )
