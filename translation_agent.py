#!/usr/bin/env python3
"""
Agente de IA para tradução de literatura cristã protestante (séculos XVI–XIX)
do inglês arcaico para o português.

Formatos suportados: .txt, .pdf (texto e imagem/escaneado), .docx

Uso:
    python translation_agent.py input.txt
    python translation_agent.py input.pdf -o saida.txt
    python translation_agent.py input.docx -o saida.txt
"""

import argparse
import base64
import io
import re
import sys
from collections.abc import Callable
from pathlib import Path

import anthropic

# ---------------------------------------------------------------------------
# Prompt de sistema: contexto teológico e diretrizes de tradução
# ---------------------------------------------------------------------------
SYSTEM_PROMPT = """Você é um tradutor especializado em literatura cristã protestante dos séculos XVI ao XIX.
Sua missão é traduzir textos do inglês arcaico para o português, preservando com fidelidade o sentido
teológico, o estilo erudito e a profundidade espiritual dos autores reformados e puritanos.

## AUTORES E TRADIÇÃO
Você conhece profundamente autores como João Calvino, John Owen, Thomas Watson, Richard Baxter,
Jonathan Edwards, Charles Spurgeon, John Bunyan, Matthew Henry, William Perkins, Thomas Brooks,
Jeremiah Burroughs, Stephen Charnock, Samuel Rutherford, George Whitefield, William Tyndale,
Thomas Cranmer, entre outros da tradição reformada e puritana.

## LÍNGUA ARCAICA INGLESA
Traduza corretamente formas verbais e pronomes arcaicos:
- "thou / thee / thy / thine" → "tu / te / teu / tua" (ou "vós" em contexto solene)
- "dost / doest" → "fazes"
- "hath / hast" → "tem / tens"
- "art" (verbo) → "és"
- "wouldst / shouldst / couldst" → "quererias / deverias / poderias"
- "verily / forsooth" → "em verdade / na verdade"
- "withal" → "além disso / também"
- "behold" → "eis / vede"

## FIDELIDADE TEOLÓGICA
Preserve a precisão doutrinária dos termos abaixo; use os equivalentes consolidados
na tradição evangélica reformada brasileira:

| Inglês                        | Português                                  |
|-------------------------------|--------------------------------------------|
| justification                 | justificação                               |
| sanctification                | santificação                               |
| election / predestination     | eleição / predestinação                    |
| atonement                     | expiação                                   |
| propitiation                  | propiciação                                |
| covenant (of grace / works)   | aliança (da graça / das obras)             |
| imputation                    | imputação                                  |
| regeneration                  | regeneração                                |
| effectual calling             | chamado eficaz                             |
| perseverance of the saints    | perseverança dos santos                    |
| total depravity               | depravação total                           |
| unconditional election        | eleição incondicional                      |
| limited atonement             | expiação particular / limitada             |
| irresistible grace            | graça irresistível                         |
| reprobation                   | reprovação                                 |
| federal headship              | cabeça federal                             |
| hypostatic union              | união hipostática                          |
| penal substitution            | substituição penal                         |
| satisfaction                  | satisfação (vicária)                       |
| mortification (of sin)        | mortificação (do pecado)                   |
| vivification                  | vivificação                                |
| assurance (of salvation)      | assurance / certeza da salvação            |
| means of grace                | meios de graça                             |
| Word, Sacraments, Prayer      | Palavra, Sacramentos, Oração               |
| ordinances                    | ordenanças                                 |
| godliness                     | piedade / santidade                        |
| sin / sinfulness              | pecado / pecaminosidade                    |
| wrath of God                  | ira de Deus                                |
| mercy / grace                 | misericórdia / graça                       |
| mediator                      | mediador                                   |
| intercession                  | intercessão                                |
| soul / spirit                 | alma / espírito                            |

## TERMOS LATINOS
Mantenha termos latinos doutrinários na primeira ocorrência com tradução entre parênteses:
- sola fide (somente pela fé)
- sola scriptura (somente pela Escritura)
- solus Christus (somente Cristo)
- sola gratia (somente pela graça)
- soli Deo gloria (somente a Deus a glória)
- ordo salutis (ordem da salvação)
- imago Dei (imagem de Deus)
- fiducia (confiança/fé fiduciária)
- cognitio Dei (conhecimento de Deus)
- sensus divinitatis (senso da divindade)
- testimonium Spiritus Sancti (testemunho do Espírito Santo)

## REFERÊNCIAS BÍBLICAS
- Use preferencialmente o vocabulário da Almeida Revista e Corrigida (ARC) ou Almeida Fiel
  quando citar ou aludir a versículos, pois são as mais compatíveis com o registro teológico reformado
- Normalize referências no formato: "João 3.16", "Romanos 8.28", "Salmos 23.1"

## ESTILO E REGISTRO
- Mantenha o estilo formal, elevado e erudito característico da literatura puritana
- Preserve períodos longos e complexos — a profundidade retórica é parte integrante da mensagem
- Conserve figuras de linguagem, paradoxos teológicos e ilustrações típicas do púlpito puritano
- Não simplifique doutrinas por dificuldade de tradução — a precisão é mais importante que a fluência
- Quando houver ambiguidade teológica, prefira a interpretação calvinista/reformada

## ESTRUTURA E FORMATAÇÃO
- Produza APENAS o texto traduzido, sem comentários, notas ou explicações adicionais
- Preserve títulos, subtítulos, numeração de capítulos/seções, pontuação e parágrafos do original
- Não acrescente nem suprima conteúdo — traduza o que está escrito

## O QUE NÃO FAZER
- Não modernize a linguagem desnecessariamente
- Não substitua a ARC por traduções modernas (NVI, NVT, etc.) nas citações bíblicas
- Não neutralize terminologia calvinista ou puritana
- Não adicione notas de rodapé, colchetes explicativos nem comentários pessoais"""

# ---------------------------------------------------------------------------
# Configurações
# ---------------------------------------------------------------------------
CHUNK_SIZE = 4000   # caracteres por trecho (~600–700 palavras)
CONTEXT_TAIL = 400  # caracteres finais da tradução anterior passados como contexto
IMAGE_SCALE = 2.0   # fator de escala para renderização de páginas PDF
IMAGE_CHARS_THRESHOLD = 80  # média de chars/página abaixo da qual o PDF é tratado como imagem


# ---------------------------------------------------------------------------
# Extração de conteúdo por formato
# ---------------------------------------------------------------------------

def extract_text_from_txt(file_bytes: bytes) -> str:
    return file_bytes.decode("utf-8", errors="replace")


def extract_text_from_docx(file_bytes: bytes) -> str:
    try:
        import docx
    except ImportError:
        sys.exit("❌  python-docx não instalado. Execute: pip install python-docx")

    doc = docx.Document(io.BytesIO(file_bytes))
    paragraphs = [p.text for p in doc.paragraphs if p.text.strip()]
    return "\n\n".join(paragraphs)


def _open_pdf(file_bytes: bytes):
    try:
        import fitz  # PyMuPDF
    except ImportError:
        sys.exit("❌  PyMuPDF não instalado. Execute: pip install PyMuPDF")
    return fitz.open(stream=file_bytes, filetype="pdf")


def is_image_pdf(file_bytes: bytes) -> bool:
    doc = _open_pdf(file_bytes)
    total_chars = sum(len(page.get_text()) for page in doc)
    avg = total_chars / max(len(doc), 1)
    return avg < IMAGE_CHARS_THRESHOLD


def extract_text_from_pdf(file_bytes: bytes) -> str:
    doc = _open_pdf(file_bytes)
    pages = [page.get_text() for page in doc]
    return "\n\n".join(pages)


def get_pdf_pages_as_images(file_bytes: bytes) -> list[bytes]:
    import fitz  # already checked in _open_pdf
    doc = _open_pdf(file_bytes)
    matrix = fitz.Matrix(IMAGE_SCALE, IMAGE_SCALE)
    images: list[bytes] = []
    for page in doc:
        pix = page.get_pixmap(matrix=matrix)
        images.append(pix.tobytes("png"))
    return images


def extract_content(file_bytes: bytes, filename: str) -> tuple[str, object]:
    """
    Returns ('text', str) for text-based files and text PDFs,
    or ('images', list[bytes]) for scanned/image PDFs.
    """
    ext = Path(filename).suffix.lower()
    if ext == ".txt":
        return "text", extract_text_from_txt(file_bytes)
    if ext == ".docx":
        return "text", extract_text_from_docx(file_bytes)
    if ext == ".pdf":
        if is_image_pdf(file_bytes):
            return "images", get_pdf_pages_as_images(file_bytes)
        return "text", extract_text_from_pdf(file_bytes)
    sys.exit(f"❌  Formato não suportado: {ext}. Use .txt, .pdf ou .docx")


# ---------------------------------------------------------------------------
# Divisão em trechos
# ---------------------------------------------------------------------------

def split_into_chunks(text: str, chunk_size: int = CHUNK_SIZE) -> list[str]:
    paragraphs = re.split(r"\n\s*\n", text.strip())
    chunks: list[str] = []
    current = ""

    for para in paragraphs:
        para = para.strip()
        if not para:
            continue

        if len(current) + len(para) + 2 > chunk_size and current:
            chunks.append(current.strip())
            current = para
        else:
            current = f"{current}\n\n{para}".strip() if current else para

        while len(current) > chunk_size * 1.5:
            sentences = re.split(r"(?<=[.!?;])\s+", current)
            if len(sentences) <= 1:
                break
            half = len(sentences) // 2
            chunks.append(" ".join(sentences[:half]).strip())
            current = " ".join(sentences[half:]).strip()

    if current.strip():
        chunks.append(current.strip())

    return chunks


# ---------------------------------------------------------------------------
# Tradução de trechos de texto
# ---------------------------------------------------------------------------

def translate_chunk(
    client: anthropic.Anthropic,
    chunk: str,
    chunk_num: int,
    total_chunks: int,
    previous_translation: str = "",
    on_delta: Callable[[str], None] | None = None,
) -> str:
    if previous_translation:
        user_content = (
            f"CONTEXTO — final da tradução do trecho anterior "
            f"(use apenas para manter coerência terminológica e estilística; NÃO traduza este bloco):\n"
            f"«{previous_translation}»\n\n"
            f"TEXTO A TRADUZIR AGORA:\n\n{chunk}"
        )
    else:
        user_content = chunk

    if on_delta is None:
        print(f"\n📖  Trecho {chunk_num}/{total_chunks}", flush=True)
        print("─" * 64)

    parts: list[str] = []

    with client.messages.stream(
        model="claude-opus-4-7",
        max_tokens=8192,
        thinking={"type": "adaptive"},
        system=[
            {
                "type": "text",
                "text": SYSTEM_PROMPT,
                "cache_control": {"type": "ephemeral"},
            }
        ],
        messages=[{"role": "user", "content": user_content}],
    ) as stream:
        for event in stream:
            if (
                event.type == "content_block_delta"
                and event.delta.type == "text_delta"
            ):
                text = event.delta.text
                parts.append(text)
                if on_delta:
                    on_delta(text)
                else:
                    print(text, end="", flush=True)

    if on_delta is None:
        print("\n" + "─" * 64)

    return "".join(parts)


# ---------------------------------------------------------------------------
# Tradução de páginas de PDF escaneado (via visão do Claude)
# ---------------------------------------------------------------------------

def translate_image_page(
    client: anthropic.Anthropic,
    page_image: bytes,
    page_num: int,
    total_pages: int,
    previous_translation: str = "",
    on_delta: Callable[[str], None] | None = None,
) -> str:
    image_b64 = base64.standard_b64encode(page_image).decode()

    context_block = ""
    if previous_translation:
        context_block = (
            f"CONTEXTO — final da tradução da página anterior "
            f"(use apenas para manter coerência terminológica e estilística; NÃO traduza este bloco):\n"
            f"«{previous_translation}»\n\n"
        )

    user_content = [
        {
            "type": "text",
            "text": (
                f"{context_block}"
                "A imagem abaixo é uma página digitalizada de um livro cristão protestante "
                "dos séculos XVI–XIX em inglês arcaico. "
                "Leia o texto da imagem com atenção e traduza-o integralmente para o português, "
                "seguindo todas as diretrizes do sistema. "
                "Produza APENAS o texto traduzido, sem comentários adicionais."
            ),
        },
        {
            "type": "image",
            "source": {
                "type": "base64",
                "media_type": "image/png",
                "data": image_b64,
            },
        },
    ]

    if on_delta is None:
        print(f"\n🖼️   Página {page_num}/{total_pages}", flush=True)
        print("─" * 64)

    parts: list[str] = []

    with client.messages.stream(
        model="claude-opus-4-7",
        max_tokens=8192,
        thinking={"type": "adaptive"},
        system=[
            {
                "type": "text",
                "text": SYSTEM_PROMPT,
                "cache_control": {"type": "ephemeral"},
            }
        ],
        messages=[{"role": "user", "content": user_content}],
    ) as stream:
        for event in stream:
            if (
                event.type == "content_block_delta"
                and event.delta.type == "text_delta"
            ):
                text = event.delta.text
                parts.append(text)
                if on_delta:
                    on_delta(text)
                else:
                    print(text, end="", flush=True)

    if on_delta is None:
        print("\n" + "─" * 64)

    return "".join(parts)


# ---------------------------------------------------------------------------
# Função principal de tradução
# ---------------------------------------------------------------------------

def translate_file(
    input_path: str,
    output_path: str | None = None,
    on_delta: Callable[[str], None] | None = None,
    on_progress: Callable[[int, int], None] | None = None,
) -> str:
    """
    Traduz um arquivo e retorna o texto traduzido completo.

    Callbacks opcionais para integração com UI:
      on_delta(text)          — chamado a cada fragmento de texto gerado
      on_progress(done, total) — chamado após cada trecho/página concluída
    """
    inp = Path(input_path)
    if not inp.exists():
        sys.exit(f"❌  Arquivo não encontrado: {input_path}")

    out = (
        Path(output_path)
        if output_path
        else inp.parent / f"{inp.stem}_pt.txt"
    )

    if on_delta is None:
        print()
        print("╔══════════════════════════════════════════════════════════════╗")
        print("║   Agente de Tradução — Literatura Cristã Reformada           ║")
        print("║   Inglês arcaico (séc. XVI–XIX)  →  Português               ║")
        print("╚══════════════════════════════════════════════════════════════╝")
        print(f"\n  Entrada : {inp}")
        print(f"  Saída   : {out}\n")

    file_bytes = inp.read_bytes()
    content_type, content = extract_content(file_bytes, inp.name)

    client = anthropic.Anthropic()
    translations: list[str] = []
    prev_translation = ""

    out.parent.mkdir(parents=True, exist_ok=True)

    if content_type == "text":
        chunks = split_into_chunks(content)
        total = len(chunks)
        if on_delta is None:
            print(f"✅  {len(content):,} caracteres divididos em {total} trecho(s)\n")

        for i, chunk in enumerate(chunks, 1):
            translated = translate_chunk(
                client=client,
                chunk=chunk,
                chunk_num=i,
                total_chunks=total,
                previous_translation=prev_translation,
                on_delta=on_delta,
            )
            translations.append(translated)
            prev_translation = translated[-CONTEXT_TAIL:] if len(translated) > CONTEXT_TAIL else translated

            out.write_text("\n\n".join(translations), encoding="utf-8")
            if on_progress:
                on_progress(i, total)
            elif on_delta is None:
                print(f"💾  Progresso salvo — {i}/{total} trecho(s) concluído(s)")

    else:  # images
        pages: list[bytes] = content
        total = len(pages)
        if on_delta is None:
            print(f"✅  PDF escaneado detectado — {total} página(s) a processar\n")

        for i, page_img in enumerate(pages, 1):
            translated = translate_image_page(
                client=client,
                page_image=page_img,
                page_num=i,
                total_pages=total,
                previous_translation=prev_translation,
                on_delta=on_delta,
            )
            translations.append(translated)
            prev_translation = translated[-CONTEXT_TAIL:] if len(translated) > CONTEXT_TAIL else translated

            out.write_text("\n\n".join(translations), encoding="utf-8")
            if on_progress:
                on_progress(i, total)
            elif on_delta is None:
                print(f"💾  Progresso salvo — {i}/{total} página(s) concluída(s)")

    full_translation = "\n\n".join(translations)

    if on_delta is None:
        print()
        print("╔══════════════════════════════════════════════════════════════╗")
        print("║   ✅  Tradução concluída com sucesso!                        ║")
        print("╚══════════════════════════════════════════════════════════════╝")
        print(f"\n  Arquivo salvo em: {out}\n")

    return full_translation


# ---------------------------------------------------------------------------
# Ponto de entrada CLI
# ---------------------------------------------------------------------------

def main() -> None:
    parser = argparse.ArgumentParser(
        description=(
            "Traduz literatura cristã protestante (séc. XVI–XIX) "
            "do inglês arcaico para o português."
        ),
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
exemplos:
  python translation_agent.py "pilgrims_progress.txt"
  python translation_agent.py "body_of_divinity.pdf" -o "corpo_da_divindade.txt"
  python translation_agent.py "institutes.docx" -o "institutas.txt"
        """,
    )
    parser.add_argument("input", help="Arquivo .txt, .pdf ou .docx a ser traduzido")
    parser.add_argument(
        "-o", "--output",
        help="Arquivo de saída (padrão: <input>_pt.txt)",
        default=None,
    )
    args = parser.parse_args()
    translate_file(args.input, args.output)


if __name__ == "__main__":
    main()
