# Guía de Alojamiento y Servidor (WebGL) - OneShotArena

Esta guía explica cómo preparar el juego para ser jugado desde un navegador web de forma gratuita.

## 1. Configuración de Unity Transport (WebSockets)
Para que WebGL funcione con Unity Relay/Lobby, el transporte debe usar WebSockets.
1. En la escena **Home**, selecciona el objeto que tiene el `UnityTransport`.
2. En el Inspector, cambia el **Protocol** de `UDP` a `WSS` (WebSockets Secure) o `WS`.
   * *Nota:* Si usas un sitio seguro (HTTPS) como GitHub Pages, DEBES usar `WSS`.
3. Asegúrate de que el campo `Wss Certificate Path` esté vacío si usas Unity Relay, ya que ellos manejan el certificado.

## 2. Build para WebGL
1. Ve a `File > Build Settings`.
2. Selecciona **WebGL** y haz clic en `Switch Platform`.
3. En `Player Settings > Publishing Settings`:
   * Desactiva **Compression Format** (ponlo en `Disabled`) para evitar errores de descompresión en servidores gratuitos como GitHub Pages, o asegúrate de que el servidor soporte Gzip/Brotli.
   * Activa **Decompression Fallback** si decides usar compresión.

## 3. Alojamiento Gratuito

### Opción A: GitHub Pages (Recomendado para la universidad)
1. Crea un repositorio en GitHub.
2. Sube el contenido de tu carpeta de Build (el `index.html`, la carpeta `Build` y `TemplateData`).
3. Ve a `Settings > Pages` en el repositorio.
4. En **Source**, selecciona `Deploy from a branch` y elige la rama `main` (o donde subiste los archivos).
5. Tu juego estará disponible en `https://tu-usuario.github.io/nombre-del-repo/`.

### Opción B: Itch.io
1. Crea una cuenta en [itch.io](https://itch.io).
2. Crea un nuevo proyecto (`Create new project`).
3. En **Kind of project**, selecciona `HTML`.
4. Comprime tu carpeta de Build en un archivo `.zip` y súbelo.
5. Activa la opción `This file will be played in the browser`.

## 4. Servidor "Siempre en Línea"
Al usar **Unity Relay y Lobby**, no necesitas un servidor dedicado encendido las 24/7.
* El primer jugador que entra crea el Lobby y se convierte en el **Host**.
* Los demás se unen a través del Lobby.
* Mientras haya al menos un jugador (el Host), la partida sigue en pie.
* **Ventaja:** Es 100% gratis hasta cierto límite de usuarios (muy alto para un proyecto de clase).

## 5. Notas Importantes para WebGL
* **Autenticación Anónima:** Ya está implementada en el código. Esto permite que los jugadores entren sin crearse una cuenta de Unity.
* **Rendimiento:** WebGL es más exigente que un ejecutable. Asegúrate de probar el juego en navegadores modernos como Chrome o Firefox.
