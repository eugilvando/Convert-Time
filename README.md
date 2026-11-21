# Convert Time

Aplicação para Windows que converte imagens, áudio, vídeo e documentos. Abra **Convert Time** no menu Iniciar, adicione os ficheiros e escolha um perfil compatível com todo o lote. Pode ajustar as opções do perfil para essa conversão, escolher a pasta de destino e acompanhar o progresso.

[Transferir o instalador para Windows x64](https://github.com/eugilvando/My-File-Converter/releases) · [Comunicar um problema](https://github.com/eugilvando/My-File-Converter/issues)

O menu de contexto clássico permite converter diretamente ou abrir a seleção na aplicação. No Windows 11 encontra estas ações em **Mostrar mais opções**. Em **Tema**, escolha Sistema, Claro ou Escuro. A conversão de documentos Office depende da aplicação Microsoft Office correspondente instalada.

Convert Time é uma derivação de [File Converter, de Adrien Allard](https://github.com/Tichau/FileConverter), distribuída sob GPLv3. Os agradecimentos e as informações sobre as dependências encontram-se abaixo.

## Setup development environment

### Requirements

For File Converter and its explorer extension:

* Visual Studio 2022

For the installer:

* [Wix 5](http://wixtoolset.org/) (will be installed by nuget)
  * [Community Visual Studio Extension](https://marketplace.visualstudio.com/items?itemName=FireGiant.FireGiantHeatWaveDev17)
* [Windows SDK Signing Tools for Desktop Apps](https://developer.microsoft.com/fr-fr/windows/downloads/windows-10-sdk)

## Thanks

Thanks to all the contributors of File Converter project.

### Localization

* Thanks to **Khidreal** and **hugok79** for the Portuguese localization.
* Thanks to **Marhc** for the Brazilian localization.
* Thanks to **Chachak** for the Spanish localization.
* Thanks to **Davide** for the Italian localization.
* Thanks to **nikotschierske** for the German localization.
* Thanks to **Snoopy1866** for the Simplified Chinese localization.
* Thanks to **MayaC0re** for the Turkish localization.
* Thanks to **vishveshjain** for the Hindi localization.
* Thanks to **Mahmoud0Sultan** for the Arabic localization.
* Thanks to **Sedimentary-Rock**, **NeKoOuO** and **PeterDaveHello** for the Traditional Chinese localization.
* Thanks to **CrisBalGreece** for the Greek localization.
* Thanks to **AshiVered** for the Hebrew localization.
* Thanks to **MrHero118** and **Mehrdad32** for the Persian localization.
* Thanks to **crnobog69** for the Serbian localizations.
* Thanks to **oogamiyuta** for the Japanese localization.
* Thanks to **AidyTheWeird** for the Czech localization.
* Thanks to **Alanimdeo** for the Korean localization.
* Thanks to **vrykolakas166** and **thaovd** for the Vietnamese localization.
* Thanks to **iliamak** for the Russian localization.
* Thanks to **itsmefdil** for the Indonesian localization.
* Thanks to **hamzaharoon1314** for the Urdu localization.
* Thanks to **Zyvrec7** and **stohlferenc** for the Hungarian localization.
* Thanks to **Maerek** and **MrPrince419** for the Polish localization.
* Thanks to **rkalitta** for the Swedish localization.

## Middlewares

File converter uses the following middlewares:

**ffmpeg** (v8.0.1) as file conversion software.
Thanks to ffmpeg devs for this awesome open source file conversion tool. [Web site link](https://ffmpeg.org)

**ImageMagick** (v14.10) as image edition and conversion software.
Thanks to image magick devs for this awesome open source image edition software suite.  [Web site link](http://imagemagick.net)
And thanks to dlemstra for the C# wrapper of this software. [Github link](https://github.com/ImageMagick/ImageMagick)

**Ghostscript** (10.02.1) as pdf edition software.
Thanks to ghostscript devs. [Download link](https://www.ghostscript.com/download/gsdnld.html)

**SharpShell** to easily create windows context menu extensions.
Thanks to Dave Kerr for his work on SharpShell. [GitHub link](https://github.com/dwmkerr/sharpshell)

**Ripper** and **yeti.mmedia** for CD Audio extraction.
Thanks to Idael Cardoso for his work on CD Audio ripper. [Code project link](https://www.codeproject.com/Articles/5458/C-Sharp-Ripper)

**Markdown.XAML** for markdown rendering in the wpf application.
Thanks to Bevan Arps for his work on Markdown.XAML. [GitHub link](https://github.com/theunrepentantgeek/Markdown.XAML)

**WpfAnimatedGif** for animated gif rendering in the wpf application.
Thanks to Thomas Levesque for his work on WpfAnimatedGif. [GitHub link](https://github.com/XamlAnimatedGif/WpfAnimatedGif)

## License

File Converter is licensed under the GPL version 3 License.
For more information check the LICENSE.md file in your installation folder or the [gnu website](https://www.gnu.org/licenses/gpl.html).
