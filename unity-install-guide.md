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

- **Download:** https://unity.com/download
- Versão atual: Unity Hub 3.x
- Instale no SSD para melhor desempenho

---

## Passo 2 — Conta Unity (gratuita)

- **Criar conta:** https://id.unity.com
- **Ativar licença Personal:** https://unity.com/products/unity-personal
- Necessária para usar o Editor

---

## Passo 3 — Unity Editor

- **Versão recomendada:** Unity 6 LTS
- **Notas de versão:** https://unity.com/releases/editor/whats-new/6000.0.0
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

| Módulo | Link | Instalar? |
|---|---|---|
| Visual Studio Community 2022 | https://visualstudio.microsoft.com/vs/community/ | **Sim** |
| Android SDK & NDK Tools | Instalado automaticamente junto com Android Build Support | **Sim** |
| OpenJDK | Instalado automaticamente junto com Android Build Support | **Sim** |

### Extras recomendados (seu PC aguenta)

| Módulo | Finalidade |
|---|---|
| Documentation | Documentação offline |
| Language Pack (Portuguese) | Interface em português |

---

## Passo 5 — Pacotes recomendados dentro do Unity

Após criar seu primeiro projeto, instale pelo **Package Manager** (Window > Package Manager):

| Pacote | Para que serve | Documentação |
|---|---|---|
| **Input System** | Controle de teclado, mouse e gamepad moderno | https://docs.unity3d.com/Packages/com.unity.inputsystem@latest |
| **Cinemachine** | Sistema de câmeras inteligente | https://docs.unity3d.com/Packages/com.unity.cinemachine@latest |
| **TextMeshPro** | Textos e fontes de alta qualidade | https://docs.unity3d.com/Packages/com.unity.textmeshpro@latest |
| **2D Sprite** | Ferramentas para jogos 2D | https://docs.unity3d.com/Packages/com.unity.2d.sprite@latest |
| **Post Processing** | Efeitos visuais (bloom, sombras, etc.) | https://docs.unity3d.com/Packages/com.unity.postprocessing@latest |
| **Universal Render Pipeline (URP)** | Pipeline de renderização otimizado | https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest |

---

## Links de Documentacao e Aprendizado

| Recurso | Link |
|---|---|
| Documentação oficial Unity | https://docs.unity3d.com |
| Unity Learn (cursos gratuitos) | https://learn.unity.com |
| Manual do Unity | https://docs.unity3d.com/Manual/index.html |
| Scripting API (C#) | https://docs.unity3d.com/ScriptReference/index.html |
| Asset Store (assets gratuitos e pagos) | https://assetstore.unity.com |
| Forum Unity | https://forum.unity.com |
| Unity no YouTube | https://www.youtube.com/@unity |

---

## Resumo da Instalação (ordem correta)

```
1. Instalar Unity Hub          → https://unity.com/download
2. Criar conta Unity           → https://id.unity.com
3. Ativar licença Personal     → https://unity.com/products/unity-personal
4. Instalar Unity 6 LTS com os módulos:
   ├── Windows Build Support (IL2CPP)
   ├── Android Build Support
   │   ├── Android SDK & NDK Tools
   │   └── OpenJDK
   ├── WebGL Build Support
   └── Visual Studio Community → https://visualstudio.microsoft.com/vs/community/
5. Criar novo projeto (template 2D ou 3D)
6. Instalar pacotes via Package Manager
```

---

## Observações para o seu PC

- Seu **RTX 3060** suporta Ray Tracing — você pode usar iluminação realista no Unity
- Com **32 GB de RAM**, pode abrir Unity + Visual Studio + navegador sem problemas
- O **SSD de 447 GB** é suficiente para vários projetos Unity (cada projeto usa ~2-5 GB)
- Use o **HDD de 932 GB** para armazenar assets, backups e projetos antigos
