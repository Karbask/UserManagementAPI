# User Management API

API Web desarrollada con ASP.NET Core utilizando Minimal APIs.

El proyecto consiste en una API sencilla para gestionar usuarios dentro de una organización. Cada usuario tiene un identificador, nombre, email, departamento y rol.

Los datos se almacenan en memoria, por lo que se pierden al reiniciar la aplicación. En esta actividad no se ha utilizado base de datos, ya que el objetivo principal era practicar la creación de endpoints, validaciones, respuestas HTTP, depuración y middleware en ASP.NET Core.

## Endpoints principales

| Método | Endpoint | Descripción |
|---|---|---|
| GET | `/api/users` | Obtiene la lista de usuarios |
| GET | `/api/users/{id}` | Obtiene un usuario por ID |
| POST | `/api/users` | Crea un nuevo usuario |
| PUT | `/api/users/{id}` | Actualiza un usuario existente |
| DELETE | `/api/users/{id}` | Elimina un usuario |

## Respuestas HTTP utilizadas

| Código | Uso |
|---|---|
| 200 OK | Operación realizada correctamente |
| 201 Created | Usuario creado correctamente |
| 204 No Content | Usuario eliminado correctamente |
| 400 Bad Request | Datos incorrectos o error de validación |
| 401 Unauthorized | Token no válido o no enviado |
| 404 Not Found | Usuario no encontrado |
| 409 Conflict | Conflicto de datos, por ejemplo email duplicado |
| 500 Internal Server Error | Error inesperado controlado por middleware |

---

# Fase 1: Creación de la API

## Qué se hizo

En la primera fase se creó la estructura inicial de la API de gestión de usuarios.

Se trabajó con Minimal APIs dentro de `Program.cs`, usando endpoints definidos directamente con:

- `app.MapGet`
- `app.MapPost`
- `app.MapPut`
- `app.MapDelete`

Se implementaron las operaciones básicas del CRUD:

- Consultar todos los usuarios.
- Consultar un usuario concreto por ID.
- Crear un nuevo usuario.
- Actualizar un usuario existente.
- Eliminar un usuario.

También se configuró Swagger para poder probar la API desde el navegador durante el desarrollo.

La API comenzó usando usuarios almacenados en memoria, lo que permitió probar la funcionalidad sin necesidad de configurar una base de datos.

## Cambios añadidos en esta fase

En esta fase se añadió:

- La configuración inicial del proyecto.
- Swagger para documentar y probar la API.
- Una colección inicial de usuarios.
- El endpoint `GET /api/users`.
- El endpoint `GET /api/users/{id}`.
- El endpoint `POST /api/users`.
- El endpoint `PUT /api/users/{id}`.
- El endpoint `DELETE /api/users/{id}`.
- Respuestas HTTP básicas para cada operación.
- Un fichero `.http` para probar las peticiones principales.

## Uso de Copilot en esta fase

En esta fase utilicé Copilot como apoyo para revisar la estructura inicial de la API.

Me ayudó a comprobar que los endpoints estaban bien planteados y que cada operación devolvía una respuesta HTTP adecuada. Por ejemplo, `Ok` para consultas correctas, `Created` al crear un usuario, `NotFound` cuando no se encuentra un usuario y `Conflict` para controlar emails duplicados.

También me sirvió para preparar algunas peticiones de prueba en un fichero `.http`, lo que facilitó probar la API desde Visual Studio Code.

Las sugerencias se revisaron y se adaptaron al contenido trabajado en el curso, manteniendo la solución sencilla y basada en Minimal APIs dentro de `Program.cs`.

---

# Fase 2: Depuración y mejora de funcionalidad

## Qué se hizo

En la segunda fase se revisó el código para corregir varios problemas simulados de la API.

Los principales puntos revisados fueron:

- Validación de datos de entrada.
- Control de usuarios inexistentes.
- Control de emails duplicados.
- Gestión de errores no controlados.
- Mejora básica del rendimiento en el listado y búsqueda de usuarios.
- Ampliación de las pruebas para cubrir casos incorrectos.

## Cambios añadidos en esta fase

Se reforzó la validación de los datos recibidos al crear o actualizar usuarios.

Antes de validar el usuario, se limpian los espacios en blanco de los campos principales. Esto evita aceptar valores como `"   "` en campos obligatorios.

También se añadieron validaciones mediante atributos como `Required` y `EmailAddress`.

De esta forma, la API comprueba que el nombre, email, departamento y rol sean obligatorios, y que el email tenga un formato válido.

También se revisó el comportamiento cuando se intenta consultar, actualizar o eliminar un usuario que no existe. En esos casos, la API devuelve `404 Not Found`.

Además, se añadió control para evitar emails duplicados. Si se intenta crear un usuario con un email ya registrado, la API devuelve `409 Conflict`. Al actualizar un usuario, también se comprueba que el email no pertenezca a otro usuario existente.

Como mejora interna, se cambió la colección de usuarios a un `Dictionary<int, User>`. Esto permite buscar usuarios por ID de forma más directa usando `TryGetValue`.

También se añadió paginación básica al endpoint de listado:

```http
GET /api/users?skip=0&take=10
```

Con esto se evita devolver siempre todos los usuarios a la vez y se controla mejor la cantidad de datos devueltos.

Por último, se incorporó un middleware de gestión de errores para capturar excepciones inesperadas y devolver respuestas JSON más controladas.

## Pruebas añadidas en esta fase

En el fichero `.http` se añadieron pruebas para comprobar los casos de depuración más importantes:

- Obtener un usuario que no existe.
- Crear un usuario con email incorrecto.
- Crear un usuario con campos obligatorios vacíos.
- Crear un usuario con campos formados solo por espacios.
- Crear un usuario con email duplicado.
- Actualizar un usuario inexistente.
- Actualizar un usuario usando el email de otro usuario.
- Eliminar un usuario inexistente.
- Probar paginación correcta.
- Probar paginación con valores incorrectos.
- Enviar una petición sin cuerpo.
- Enviar JSON mal formado.

Estas pruebas ayudaron a comprobar que la API devolvía respuestas adecuadas como `400 Bad Request`, `404 Not Found` y `409 Conflict`.

## Uso de Copilot en esta fase

En esta fase utilicé Copilot para revisar posibles fallos en el código y detectar casos que no había contemplado inicialmente.

Me ayudó a identificar mejoras en la validación de datos, como controlar campos vacíos, emails incorrectos o valores formados solo por espacios.

También me sirvió para revisar los casos en los que se intenta consultar, actualizar o eliminar un usuario inexistente, asegurando que la API devuelva `404 Not Found` en lugar de provocar un error no controlado.

Copilot también sugirió centralizar parte de la gestión de errores mediante middleware en el pipeline de ASP.NET Core. Esta idea se adaptó al proyecto para mantener el código sencillo, pero más robusto.

Además, se amplió el fichero `.http` con pruebas de depuración, incluyendo datos inválidos, emails duplicados, peticiones sin cuerpo, JSON mal formado y usuarios inexistentes.

Las sugerencias se revisaron antes de incorporarlas para mantener la solución dentro del nivel trabajado en el curso.

---

# Fase 3: Middleware de auditoría, errores y autenticación

## Qué se hizo

En la tercera fase se añadieron nuevos middleware al pipeline de ASP.NET Core para cumplir con los requisitos de TechHive Solutions.

Los objetivos de esta fase eran:

- Registrar solicitudes entrantes y respuestas salientes.
- Aplicar una gestión de errores estandarizada.
- Proteger los endpoints mediante autenticación basada en token.
- Configurar el pipeline en el orden correcto.

## Cambios añadidos en esta fase

### Middleware de gestión de errores

Se añadió un middleware global al inicio del pipeline para capturar excepciones no controladas.

Este middleware controla errores como:

- Peticiones mal formadas.
- JSON inválido.
- Errores inesperados de la aplicación.

Cuando ocurre un error, la API devuelve una respuesta JSON con un formato coherente.

Ejemplo de respuesta:

```json
{
  "error": "Internal server error.",
  "message": "Se produjo un error inesperado al procesar la solicitud."
}
```

Esto evita que la API se bloquee o devuelva errores sin controlar.

### Middleware de autenticación

Se añadió un middleware sencillo de autenticación basada en token.

Las peticiones dirigidas a `/api` deben incluir una cabecera `Authorization` con el siguiente formato:

```http
Authorization: Bearer techhive-token-123
```

Si no se envía token, si el formato no es correcto o si el token no coincide, la API devuelve `401 Unauthorized`.

Este sistema se ha implementado de forma sencilla para la actividad del curso. En una aplicación real, el token debería gestionarse de forma más segura, por ejemplo mediante configuración o autenticación JWT.

### Middleware de registro

También se añadió un middleware para registrar información básica de auditoría.

Este middleware registra:

- Método HTTP.
- Ruta solicitada.
- Código de estado de la respuesta.
- Tiempo aproximado de procesamiento.

Esto permite revisar por consola qué peticiones se han recibido y qué respuesta ha devuelto la API.

### Orden del pipeline

El middleware se configuró en el orden solicitado en la actividad:

1. Gestión de errores.
2. Autenticación.
3. Registro.

La gestión de errores se coloca primero para poder capturar errores producidos en los siguientes pasos del pipeline.

La autenticación se ejecuta antes de llegar a los endpoints, para bloquear peticiones no autorizadas.

El registro se ejecuta después, para auditar las peticiones que continúan por el pipeline.

## Pruebas añadidas en esta fase

En el fichero `.http` se añadieron pruebas específicas para validar el middleware:

- Acceder a un endpoint con token válido.
- Acceder a un endpoint sin token.
- Acceder a un endpoint con token incorrecto.
- Acceder a un endpoint con formato de autorización incorrecto.
- Crear un usuario con token válido.
- Intentar crear un usuario sin token.
- Simular un error interno para comprobar el middleware de errores.
- Enviar JSON mal formado con token válido.

Con estas pruebas se comprobó que la API bloquea correctamente las peticiones no autorizadas y que los errores se devuelven con un formato JSON coherente.

## Uso de Copilot en esta fase

En esta fase utilicé Copilot como apoyo para plantear los middleware necesarios.

Me ayudó a preparar el middleware de registro, indicando qué datos podían ser útiles para auditoría, como el método HTTP, la ruta solicitada, el código de estado y el tiempo de respuesta.

También sirvió para revisar la estructura del middleware de gestión de errores y devolver respuestas JSON coherentes cuando ocurre una excepción.

Otra parte en la que Copilot ayudó fue en la autenticación basada en token. A partir de sus sugerencias, se implementó una validación sencilla de la cabecera `Authorization`.

También se utilizó para revisar el orden del pipeline y comprobar que los middleware se ejecutaran en una secuencia lógica y acorde al enunciado.

Finalmente, Copilot ayudó a preparar pruebas en el fichero `.http`, incluyendo peticiones con token válido, sin token, con token incorrecto y una petición para simular un error interno.

Las sugerencias se adaptaron al código existente para mantener la solución dentro de `Program.cs`, sin añadir más complejidad de la necesaria.

---

# Pruebas realizadas

Las pruebas se realizaron usando un fichero `.http`.

Se probaron casos como:

- Obtener todos los usuarios.
- Obtener un usuario por ID.
- Obtener un usuario inexistente.
- Crear usuarios válidos.
- Crear usuarios con email incorrecto.
- Crear usuarios con campos vacíos.
- Crear usuarios con email duplicado.
- Actualizar usuarios existentes.
- Intentar actualizar usuarios inexistentes.
- Eliminar usuarios.
- Intentar eliminar usuarios inexistentes.
- Probar paginación correcta e incorrecta.
- Enviar JSON mal formado.
- Enviar peticiones sin cuerpo.
- Acceder sin token.
- Acceder con token incorrecto.
- Acceder con token válido.
- Simular un error para probar el middleware de gestión de errores.

Estas pruebas sirvieron para comprobar que la API responde correctamente tanto en los casos válidos como en los casos de error.

---

# Conclusión

El proyecto terminó con una API funcional de gestión de usuarios creada con ASP.NET Core y Minimal APIs.

En la primera fase se implementó el CRUD básico.

En la segunda fase se reforzaron las validaciones, el control de errores y la búsqueda de usuarios.

En la tercera fase se añadieron middleware para auditoría, gestión de errores y autenticación por token.

Copilot se utilizó como herramienta de apoyo durante el desarrollo, principalmente para revisar código, detectar posibles errores, proponer mejoras y preparar pruebas. Las sugerencias se adaptaron al nivel del curso y al código existente, manteniendo la solución dentro de `Program.cs` y sin añadir más complejidad de la necesaria.