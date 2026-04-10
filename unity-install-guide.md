# Guia de Instalação Unity — Personalizado para Seu PC

## Configurações do PC (SE008ET082CW)

| Componente | Especificação |
|---|---|
| Processador | Intel Core i7-11700 @ 2.50GHz (8 núcleos) |
| RAM | 32 GB |
| Placa de Vídeo | NVIDIA GeForce RTX 3060 (12 GB) |
| Armazenamento | 447 GB SSD + 932 GB HDD |
| Sistema | Windows 11 64-bit |

---

## Passo 1 — Unity Hub

- Baixe em: https://unity.com/download
- Versão atual: Unity Hub 3.x
- Instale no SSD para melhor desempenho

---

## Passo 2 — Conta Unity (gratuita)

- Crie em: https://id.unity.com
- Ative a licença **Unity Personal** (gratuita)
- Necessária para usar o Editor

---

## Passo 3 — Unity Editor

- Versão recomendada: **Unity 6 LTS** (mais atual e estável)
- Instale pelo Unity Hub em: Installs > Install Editor
- Instale no SSD (447 GB ADATA SU650)

---

## Passo 4 — Módulos a instalar (junto com o Editor)

### Plataformas de Build

| Módulo | Finalidade | Instalar? |
|---|---|---|
| Windows Build Support (IL2CPP) | Publicar jogo para PC | **Sim** |
| Android Build Support | Publicar para celular Android | **Sim** |
| WebGL Build Support | Publicar para rodar no navegador | **Sim** |
| iOS Build Support | Publicar para iPhone (requer Mac) | Opcional |

### Ferramentas de Desenvolvimento

| Módulo | Finalidade | Instalar? |
|---|---|---|
| Visual Studio Community 2022 | Editor de código C# | **Sim** |
| Android SDK & NDK Tools | Necessário para build Android | **Sim** (junto com Android Build Support) |
| OpenJDK | Java para Android | **Sim** (junto com Android Build Support) |

### Extras recomendados (seu PC aguenta)

| Módulo | Finalidade |
|---|---|
| Documentation | Documentação offline |
| Language Pack (Portuguese) | Interface em português |

---

## Passo 5 — Pacotes recomendados dentro do Unity

Após criar seu primeiro projeto, instale pelo **Package Manager** (Window > Package Manager):

| Pacote | Para que serve |
|---|---|
| **Input System** | Controle de teclado, mouse e gamepad moderno |
| **Cinemachine** | Sistema de câmeras inteligente |
| **TextMeshPro** | Textos e fontes de alta qualidade |
| **2D Sprite** | Ferramentas para jogos 2D |
| **Post Processing** | Efeitos visuais (bloom, sombras, etc.) |
| **Universal Render Pipeline (URP)** | Pipeline de renderização otimizado |

---

## Resumo da Instalação (ordem correta)

```
1. Instalar Unity Hub (no SSD)
2. Criar conta em id.unity.com
3. Ativar licença Personal (gratuita)
4. Instalar Unity 6 LTS com os módulos:
   ├── Windows Build Support (IL2CPP)
   ├── Android Build Support
   │   ├── Android SDK & NDK Tools
   │   └── OpenJDK
   ├── WebGL Build Support
   └── Visual Studio Community 2022
5. Criar novo projeto (template 2D ou 3D)
6. Instalar pacotes via Package Manager
```

---

## Observações para o seu PC

- Seu **RTX 3060** suporta Ray Tracing — você pode usar iluminação realista no Unity
- Com **32 GB de RAM**, pode abrir Unity + Visual Studio + navegador sem problemas
- O **SSD de 447 GB** é suficiente para vários projetos Unity (cada projeto usa ~2-5 GB)
- Use o **HDD de 932 GB** para armazenar assets, backups e projetos antigos
