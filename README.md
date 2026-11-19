# Veterinaria API · Autenticación JWT (Semana 14)

Web API en capas (**.NET 10**, EF Core 10, AutoMapper) de la S13, ahora protegida con **JWT**: registro, login, endpoints con `[Authorize]` y roles propios del negocio. Laboratorio S14 – Desarrollo de Aplicaciones Empresariales Avanzadas (Tecsup).

## 1. Tema y roles
**Problema que resuelve:** una clínica veterinaria necesita controlar quién puede ver, registrar y borrar información de mascotas y citas. Sin autenticación cualquiera podría eliminar un historial o agendar citas falsas.

| Rol | Qué puede hacer en la clínica |
|---|---|
| **Cliente** (dueño; se asigna por defecto al registrarse) | Consultar mascotas y citas. No modifica nada. |
| **Recepcionista** | Registrar y editar mascotas, agendar y reprogramar citas. |
| **Veterinario** | Atender (actualizar) citas y eliminar mascotas/citas. |

El rol lo decide siempre el servidor: `POST api/auth/registro` crea usuarios `Cliente`; para promover a alguien se hace un `UPDATE` en la base de datos.

## 2. Matriz de permisos
| Endpoint | Sin token | Cliente | Recepcionista | Veterinario |
|---|---|---|---|---|
| POST /api/auth/registro | Sí (201 / 409) | Sí | Sí | Sí |
| POST /api/auth/login | Sí (200 / 401) | Sí | Sí | Sí |
| GET /api/auth/perfil | No (401) | Sí | Sí | Sí |
| GET /api/mascotas | **Sí (público)** | Sí | Sí | Sí |
| GET /api/mascotas/{id} · /{id}/citas | No (401) | Sí | Sí | Sí |
| GET /api/citas · /api/citas/{id} | No (401) | Sí | Sí | Sí |
| POST /api/mascotas | No (401) | No (403) | **Sí** | No (403) |
| PUT /api/mascotas/{id} | No (401) | No (403) | **Sí** | No (403) |
| POST /api/citas | No (401) | No (403) | **Sí** | No (403) |
| PUT /api/citas/{id} | No (401) | No (403) | Sí | Sí |
| DELETE /api/mascotas/{id} | No (401) | No (403) | No (403) | **Sí** |
| DELETE /api/citas/{id} | No (401) | No (403) | No (403) | **Sí** |

Públicos: `AllowAnonymous` · Solo autenticado: `[Authorize]` · Por rol: `[Authorize(Roles = "...")]`.

## 3. Arquitectura
`Api → Negocio → Datos → Entidades`

| Capa | Agregado en S14 |
|---|---|
| Entidades | `Usuario`, `Roles`, DTOs `LoginDto`, `RegistroDto` (sin rol), `TokenDto`, `PerfilDto` |
| Datos | `IUsuarioRepositorio` / `UsuarioRepositorio` (buscar por correo, agregar, guardar); `DbSet<Usuario>` |
| Negocio | `IAuthServicio` / `AuthServicio` (BCrypt + generación del JWT), `JwtOpciones`; correo repetido lanza `ReglaNegocioException` |
| Api | `AuthController`, `[Authorize]` en controladores, `AddJwtBearer`, Swagger con esquema Bearer, panel `wwwroot/index.html` |

Token: claims `sub`, `email`, `role` (+ `jti`), firmado HS256. Se validan emisor, audiencia, vencimiento y firma. `UseAuthentication` va antes de `UseAuthorization`.

## 4. Puesta en marcha
1. Ejecutar `script.sql` (crea `VeterinariaDB`, tabla `Usuario` y un usuario por rol):
   ```
   sqlcmd -S localhost\SQLEXPRESS -E -C -f 65001 -i script.sql
   ```
2. **Clave JWT (no está en el repositorio).** Copia `Veterinaria.Api/appsettings.Development.example.json` a `appsettings.Development.json` (ignorado por git) y escribe una clave de 32+ caracteres, o usa:
   ```
   dotnet user-secrets init --project Veterinaria.Api
   dotnet user-secrets set "Jwt:Key" "<clave de 32+ caracteres>" --project Veterinaria.Api
   ```
   Emisor, audiencia y minutos de vida están en `appsettings.json` (sección `Jwt`).
3. `dotnet run --project Veterinaria.Api --launch-profile http` y abrir `http://localhost:5206`:
   - `/` → panel de demostración (login, claims del token y matriz de permisos en vivo).
   - `/swagger` → Swagger con botón **Authorize** (pegar el token de `api/auth/login`).

Usuarios de prueba (clave `Clave123*`): `cliente@vet.com`, `recepcion@vet.com`, `veterinario@vet.com`.

Paquetes: `BCrypt.Net-Next` y `System.IdentityModel.Tokens.Jwt 8.*` (Negocio); `Microsoft.AspNetCore.Authentication.JwtBearer 10.*` (Api).

## 5. Pruebas
`requests.http` contiene cada caso (200, 201, 204, 400, 401, 403, 404, 409) con un token por rol. También se pueden ejecutar desde el panel `/`.

## 6. Sustentación de seguridad
**¿Por qué 401 y 403?** 401 = la API no sabe quién eres (sin token, token vencido o inválido, o login con credenciales malas). Ejemplo: `GET /api/mascotas/1` sin token → 401. 403 = sabe quién eres pero tu rol no alcanza. Ejemplo: `POST /api/mascotas` con el token del Cliente → 403.

**Claims del token:** `sub` (identificador del usuario), `email` (para mostrar quién es y cumplir `perfil`) y `role` (para `[Authorize(Roles)]` sin consultar la base en cada petición); además `exp`, `iss`, `aud` y `jti`. Nunca pondría dentro del token contraseñas ni hashes, la clave secreta, cadenas de conexión, tarjetas, documentos u otros datos sensibles: el payload solo está codificado en Base64, cualquiera lo lee (jwt.io); la firma garantiza que no se altere, no que sea secreto.

**Contraseña en texto plano:** si se filtra la base de datos (SQL injection, copia de seguridad, un empleado) el atacante obtiene todas las claves y puede entrar a esas cuentas y a otros sitios donde el usuario reutilizó la clave. Con BCrypt solo hay hashes con sal y costo, lentos de romper por fuerza bruta, y ni el administrador conoce la clave.

**¿Por qué el rol sale del token firmado?** Si el cliente enviara su rol en el registro, cualquiera se registraría como Veterinario (escalada de privilegios). El servidor asigna el rol y lo escribe en un token firmado con la clave secreta; si el cliente modifica el claim, la firma deja de coincidir y el token se rechaza (401).

## Autor
Arévalo Sermeño, Edwin William – Sección C, Tecsup.
