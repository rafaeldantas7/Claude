#!/usr/bin/env python3
"""
Agente de IA para tradução de literatura cristã protestante (séculos XVI–XIX)
do inglês arcaico para o português.

Uso:
    python translation_agent.py input.txt
    python translation_agent.py input.txt -o saida.txt
"""

import argparse
import re
import sys
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
# Configurações de chunking
# ---------------------------------------------------------------------------
CHUNK_SIZE = 4000   # caracteres por trecho (~600–700 palavras)
CONTEXT_TAIL = 400  # caracteres finais da tradução anterior passados como contexto


# ---------------------------------------------------------------------------
# Funções auxiliares
# ---------------------------------------------------------------------------

def split_into_chunks(text: str, chunk_size: int = CHUNK_SIZE) -> list[str]:
    """Divide o texto em trechos respeitando limites de parágrafos."""
    paragraphs = re.split(r"\n\s*\n", text.strip())
    chunks: list[str] = []
    current = ""

    for para in paragraphs:
        para = para.strip()
        if not para:
            continue

        if len(current) + len(para) + 2 > chunk_size and current:
            # Salva o trecho atual e começa um novo
            chunks.append(current.strip())
            current = para
        else:
            current = f"{current}\n\n{para}".strip() if current else para

        # Parágrafo único excepcionalmente longo: divide por sentenças
        while len(current) > chunk_size * 1.5:
            sentences = re.split(r"(?<=[.!?;])\s+", current)
            if len(sentences) <= 1:
                break  # impossível dividir mais — aceita o trecho longo
            half = len(sentences) // 2
            chunks.append(" ".join(sentences[:half]).strip())
            current = " ".join(sentences[half:]).strip()

    if current.strip():
        chunks.append(current.strip())

    return chunks


def translate_chunk(
    client: anthropic.Anthropic,
    chunk: str,
    chunk_num: int,
    total_chunks: int,
    previous_translation: str = "",
) -> str:
    """Traduz um trecho com streaming, usando contexto do trecho anterior."""

    if previous_translation:
        user_content = (
            f"CONTEXTO — final da tradução do trecho anterior "
            f"(use apenas para manter coerência terminológica e estilística; NÃO traduza este bloco):\n"
            f"«{previous_translation}»\n\n"
            f"TEXTO A TRADUZIR AGORA:\n\n{chunk}"
        )
    else:
        user_content = chunk

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
                "cache_control": {"type": "ephemeral"},  # Cache do prompt de sistema
            }
        ],
        messages=[{"role": "user", "content": user_content}],
    ) as stream:
        for event in stream:
            if (
                event.type == "content_block_delta"
                and event.delta.type == "text_delta"
            ):
                print(event.delta.text, end="", flush=True)
                parts.append(event.delta.text)

    print("\n" + "─" * 64)
    return "".join(parts)


# ---------------------------------------------------------------------------
# Função principal
# ---------------------------------------------------------------------------

def translate_file(input_path: str, output_path: str | None = None) -> None:
    """Lê um arquivo .txt em inglês e salva a tradução em português."""
    inp = Path(input_path)
    if not inp.exists():
        sys.exit(f"❌  Arquivo não encontrado: {input_path}")

    out = (
        Path(output_path)
        if output_path
        else inp.parent / f"{inp.stem}_pt{inp.suffix}"
    )

    print()
    print("╔══════════════════════════════════════════════════════════════╗")
    print("║   Agente de Tradução — Literatura Cristã Reformada           ║")
    print("║   Inglês arcaico (séc. XVI–XIX)  →  Português               ║")
    print("╚══════════════════════════════════════════════════════════════╝")
    print(f"\n  Entrada : {inp}")
    print(f"  Saída   : {out}\n")

    text = inp.read_text(encoding="utf-8")
    chunks = split_into_chunks(text)
    total = len(chunks)
    print(f"✅  {len(text):,} caracteres divididos em {total} trecho(s)\n")

    client = anthropic.Anthropic()
    translations: list[str] = []
    prev_translation = ""

    out.parent.mkdir(parents=True, exist_ok=True)

    for i, chunk in enumerate(chunks, 1):
        translated = translate_chunk(
            client=client,
            chunk=chunk,
            chunk_num=i,
            total_chunks=total,
            previous_translation=prev_translation,
        )
        translations.append(translated)

        # Atualiza contexto para o próximo trecho
        prev_translation = (
            translated[-CONTEXT_TAIL:]
            if len(translated) > CONTEXT_TAIL
            else translated
        )

        # Salva progresso incremental após cada trecho
        out.write_text("\n\n".join(translations), encoding="utf-8")
        print(f"💾  Progresso salvo — {i}/{total} trecho(s) concluído(s)")

    print()
    print("╔══════════════════════════════════════════════════════════════╗")
    print("║   ✅  Tradução concluída com sucesso!                        ║")
    print("╚══════════════════════════════════════════════════════════════╝")
    print(f"\n  Arquivo salvo em: {out}\n")


# ---------------------------------------------------------------------------
# Ponto de entrada
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
  python translation_agent.py "body_of_divinity.txt" -o "corpo_da_divindade.txt"
        """,
    )
    parser.add_argument("input", help="Arquivo .txt em inglês a ser traduzido")
    parser.add_argument(
        "-o", "--output",
        help="Arquivo de saída (padrão: <input>_pt.txt)",
        default=None,
    )
    args = parser.parse_args()
    translate_file(args.input, args.output)


if __name__ == "__main__":
    main()
