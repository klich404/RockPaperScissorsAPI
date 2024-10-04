# RockPaperScissorsAPI

## Descripción

RockPaperScissorsAPI es una aplicación de juego de "Piedra, Papel o Tijera" que permite a los jugadores competir entre sí. Esta API maneja la lógica del juego, el registro de jugadores y la contabilización de rondas ganadas.

## Requisitos

Para ejecutar el backend de este proyecto, asegúrate de tener instalado:

- [.NET 6](https://dotnet.microsoft.com/download/dotnet/6.0) o superior.
- Paquetes de NuGet recomendados por Visual Studio.

## Configuración

### Puerto de Ejecución

Antes de ejecutar la aplicación, asegúrate de establecer el puerto de ejecución deseado. Para ello, edita el archivo `launchSettings.json` en la siguiente ruta:

Properties/launchSettings.json

Dentro de este archivo, busca la sección `profiles` y modifica el valor de `applicationUrl` bajo `http` para definir el puerto que deseas usar.

### Base de Datos

Debes asegurarte de descargar la base de datos que se incluye en este proyecto. Luego, ajusta la dirección de la base de datos en el archivo `appsettings.json`. Busca la sección `ConnectionStrings` y actualiza el valor de `RockPaperScissorsCon` con la ruta correcta a tu base de datos.

## Frontend

El frontend de este proyecto se encuentra en el siguiente repositorio de GitHub:

[RockPaperScissorsFront](https://github.com/klich404/RockPaperScissorsFront)

## Uso

1. Configura el entorno según las instrucciones anteriores.
2. Inicia el servidor backend.
3. Accede a la interfaz del frontend para comenzar a jugar.

## Contribuciones

Las contribuciones son bienvenidas. Si deseas contribuir, por favor abre un issue o envía un pull request.

## Licencia

Este proyecto está bajo la licencia MIT.

