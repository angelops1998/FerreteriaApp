# FerreteriaApp — Sistema de Ferretería con ASP.NET Core MVC (.NET 10) y Supabase

Sistema web completo para una ferretería: catálogo de productos con imágenes, carrito de compras,
pedidos y un panel de administración. Desarrollado con **ASP.NET Core MVC**, **Entity Framework Core**,
**PostgreSQL en Supabase** y desplegado en **Render** con Docker.

> **¿Nuevo en ASP.NET?** Leé `INSTRUCTIVO.md` (también en `INSTRUCTIVO.docx` / `INSTRUCTIVO.pdf`):
> explica todos los conceptos y el proyecto archivo por archivo, paso a paso.

---

## 1. Funcionalidades

El sistema implementa el **diagrama de clases UML de la ferretería** (ver sección 3.1): usuarios con
roles y herencia Empleado/Cliente, categorías, proveedores, productos con inventario, ventas con
detalle y forma de pago, y compras a proveedores con detalle.

### Público (sin iniciar sesión)
- Página de inicio con categorías y productos destacados.
- Catálogo con **búsqueda**, **filtro por categoría** y **paginación**.
- **Búsqueda por voz** (botón de micrófono junto a cada buscador) con la Web Speech API. Funciona en
  Chrome y Edge (usan el reconocimiento de voz de Google/Microsoft); requiere permitir el micrófono y
  que el sitio esté en `https` o `localhost`. En navegadores sin soporte (Firefox) el botón no aparece.
- Detalle de producto con productos relacionados.
- **App instalable (PWA)**: se puede agregar a la pantalla de inicio del celular (o instalar en la
  PC) sin pasar por la Play Store; se abre a pantalla completa, sin la barra del navegador, con su
  propio ícono. Ver sección 8.4.
- **Carrito** (se guarda en la sesión; no hace falta cuenta para armarlo).
- Registro (crea un **Cliente**) e inicio de sesión.

### Cliente (rol `Cliente`)
- **Finalizar compra** eligiendo la forma de pago: se genera una **Venta** en estado *Pendiente* con
  sus **DetalleVenta** y se descuenta el stock (todo en una transacción).
- **Mis compras**: historial con estados y detalle. Puede cancelar mientras esté *Pendiente*.
- Editar su perfil y cambiar la contraseña.

### Empleado (rol `Empleado`)
- **Panel general** con ventas, compras, stock bajo y productos más vendidos.
- **Registrar venta de mostrador** (elige cliente, forma de pago y productos): queda *Completada*
  a nombre del empleado.
- **Gestionar ventas**: filtrar por estado y cambiarlo
  (*Pendiente → Completada → Devuelta* / *Cancelada*). Cancelar o devolver repone el stock.
- **Registrar compras a proveedores** con detalle; al completarlas ingresa la mercadería (sube el
  stock y actualiza el precio de compra). No se permite cancelar una compra si dejaría stock negativo.
- **Inventario**: stock actual, mínimo, última actualización, filtro "bajo mínimo" y ajuste manual.
- Crear y editar **productos** (con subida de imagen).
- **Reportes** (menú Gestión → Reportes), todos con **gráficos** (Chart.js), filtros por **fecha** y
  botones **Descargar PDF** (imprimir → "Guardar como PDF") y **Exportar CSV** (Excel):
  - **Ventas**: por período, estado y forma de pago; evolución por día, por empleado y por estado.
  - **Compras a proveedores**: por período, estado y proveedor.
  - **Stock e inventario**: stock valorizado a costo y a venta, por categoría, bajo mínimo y sin stock.
  - **Productos vendidos**: ranking por unidades, ingresos o ganancia estimada, e ingresos vs. costo.
  - **Usuarios**: administradores, empleados y clientes; rol, estado, altas por mes y compras de cada cliente.
  - **Resumen financiero**: ventas cobradas vs. compras pagadas, mes a mes.

### Administrador (rol `Admin`)
- Todo lo del empleado, más: **categorías**, **proveedores**, **formas de pago**, eliminar productos,
  y **usuarios**: alta/edición de empleados, rol Admin/Empleado, activar/desactivar cuentas y tipo de
  cliente (Regular/Mayorista).

### Validaciones (según el diagrama)
- Email único y con formato válido. Contraseña de **mínimo 8 caracteres con mayúsculas, minúsculas y
  números**. Teléfono solo números de **8 a 15 dígitos**. Estado no nulo.
- Producto: nombre obligatorio (máx. 100), precios de venta y compra > 0, stock y stock mínimo ≥ 0,
  categoría y proveedor obligatorios.
- Venta/Compra: fecha no nula, total > 0, cantidad > 0, precio unitario > 0, no vender sin stock,
  no dejar stock negativo.

### Datos iniciales (seed automático)
La primera vez que arranca, la app crea sola los roles, el administrador, un vendedor de ejemplo,
4 formas de pago, 8 categorías, 6 proveedores, los **43 productos** con sus imágenes y su
**inventario** inicial.

---

## 2. Tecnologías

| Componente | Tecnología |
|---|---|
| Framework web | ASP.NET Core MVC — .NET 10 (LTS) |
| Acceso a datos | Entity Framework Core 10 + Npgsql |
| Base de datos | PostgreSQL (Supabase) |
| Autenticación | ASP.NET Core Identity con claves `int` (cookies, roles `Admin`, `Empleado`, `Cliente`) |
| Carrito | Sesión de ASP.NET Core (no es una tabla) |
| Front-end | Razor Views + Bootstrap 5 + Bootstrap Icons |
| Deploy | Docker en Render |

---

## 3. Estructura del proyecto

```
FerreteriaApp/
├── Controllers/
│   ├── HomeController.cs          Inicio, página de error y manifest de la PWA (/manifest.webmanifest)
│   ├── AccountController.cs       Login, registro (Cliente), logout, perfil, cambiar contraseña
│   ├── ProductosController.cs     Catálogo público + CRUD de productos (personal)
│   ├── CategoriasController.cs    CRUD de categorías (Admin)
│   ├── ProveedoresController.cs   CRUD de proveedores (Admin)
│   ├── FormasPagoController.cs    CRUD de formas de pago (Admin)
│   ├── CarritoController.cs       Carrito en sesión (agregar, actualizar, quitar, vaciar)
│   ├── VentasController.cs        Checkout web, mis compras, venta de mostrador, gestión de estados
│   ├── ComprasController.cs       Compras a proveedores y su ingreso al stock
│   ├── InventarioController.cs    Inventario y ajustes manuales
│   ├── AdminController.cs         Panel general, usuarios y empleados
│   └── ReportesController.cs      Reportes (ventas, compras, inventario, productos, clientes, financiero) + CSV
├── Models/
│   ├── Usuario.cs                 Base de Identity (int) + nombre, apellido, estado, fechaCreacion
│   ├── Rol.cs                     Rol de Identity (int) + descripción
│   ├── Empleado.cs / Cliente.cs   Heredan de Usuario (tablas Empleados y Clientes)
│   ├── Categoria.cs, Proveedor.cs, Producto.cs, Inventario.cs, FormaPago.cs
│   ├── Venta.cs (+ enum EstadoVenta), DetalleVenta.cs
│   ├── Compra.cs (+ enum EstadoCompra), DetalleCompra.cs
│   ├── CarritoItem.cs             Ítem del carrito (en sesión)
│   ├── *ViewModel.cs              Formularios (login, registro, perfil, checkout, venta, compra, empleado)
│   └── Reporte*ViewModel.cs, ResumenFila.cs   Datos que muestra cada reporte (ventas, compras, stock, productos, usuarios, financiero)
├── Data/
│   ├── ApplicationDbContext.cs    Único DbContext (Identity + tablas de la ferretería)
│   └── DbInitializer.cs           Seed: roles, admin, vendedor, formas de pago, categorías, proveedores, productos, inventario
├── Helpers/
│   ├── Formato.cs                 Precios, fechas (UTC ↔ hora local) y colores de estado
│   ├── Csv.cs                     Genera los archivos CSV de los reportes
│   ├── CarritoSesion.cs           Leer/guardar el carrito en la sesión
│   ├── UsuarioExtensions.cs       Id numérico del usuario logueado, EsPersonal()
│   └── SpanishIdentityErrorDescriber.cs   Mensajes de Identity en español
├── Migrations/                    Migraciones de EF Core (ya generadas)
├── Views/                         Vistas Razor (Home, Account, Productos, Categorias, Proveedores, FormasPago, Carrito, Ventas, Compras, Inventario, Admin, Reportes, Shared)
├── wwwroot/
│   ├── css/site.css               Estilos propios (incluye los estilos de impresión de los reportes)
│   ├── js/reportes.js             Gráficos de los reportes (Chart.js) e impresión a PDF
│   ├── js/site.js                 Búsqueda por voz (Web Speech API) y botón "Instalar app" (PWA)
│   ├── sw.js                      Service worker de la PWA (caché de archivos y página sin conexión)
│   ├── offline.html               Página que se muestra si se abre la app sin internet
│   ├── icons/                     Íconos de la app instalada (192, 512, maskable y Apple)
│   ├── lib/chart.js/              Chart.js (librería de gráficos)
│   └── images/productos/          Imágenes de los productos
├── appsettings.json               Configuración (connection string, nombre de la tienda, admin)
├── Program.cs
├── Dockerfile                     Para el deploy en Render
└── render.yaml                    Blueprint de Render
```

### 3.1 Del diagrama de clases a las tablas

| Clase del diagrama | Tabla | Notas |
|---|---|---|
| `Usuario` | `Usuarios` | Es el usuario de ASP.NET Core Identity con clave `int` (`IdUsuario`). Identity aporta `Email`, `PasswordHash` (la contraseña nunca se guarda en texto plano) y las columnas de seguridad. Se agregan `Nombre`, `Apellido`, `Estado`, `FechaCreacion`. |
| `Rol` | `Roles` | Rol de Identity (`IdRol`, `Nombre`, `Descripcion`). La relación Usuario–Rol queda en `UsuarioRoles`; la app asigna **un** rol por usuario (`Admin`, `Empleado` o `Cliente`). |
| `Empleado` hereda `Usuario` | `Empleados` | Herencia **TPT** (una tabla por tipo): `IdEmpleado` es PK y a la vez FK a `Usuarios.IdUsuario`. Campos: `Cargo`, `Telefono`, `Direccion`. |
| `Cliente` hereda `Usuario` | `Clientes` | Igual que Empleado: `IdCliente` = PK/FK. Campos: `Telefono`, `Direccion`, `TipoCliente`. |
| `Categoria` | `Categorias` | `IdCategoria`, `Nombre`, `Descripcion`, `Estado`. |
| `Proveedor` | `Proveedores` | `IdProveedor`, `Nombre`, `Ruc`, `Telefono`, `Direccion`, `Estado`. |
| `Producto` | `Productos` | `IdProducto`, `Nombre`, `Descripcion`, `PrecioVenta`, `PrecioCompra`, `Stock`, `StockMinimo`, `Estado`, `IdCategoria`, `IdProveedor` + `ImagenUrl` (campo extra para la foto). |
| `Inventario` | `Inventarios` | `IdInventario`, `IdProducto`, `StockActual`, `StockMinimo`, `UltimaActualizacion`. Uno por producto, se actualiza en cada venta, compra o ajuste. |
| `FormaPago` | `FormasPago` | `IdFormaPago`, `Nombre`, `Descripcion`, `Estado`. |
| `Venta` | `Ventas` | `IdVenta`, `Fecha`, `IdCliente`, `IdEmpleado` (nulo hasta que un empleado completa una venta web), `IdFormaPago`, `Total`, `Estado` (enum `EstadoVenta`), `Observaciones`. |
| `DetalleVenta` | `DetalleVentas` | `IdDetalleVenta`, `IdVenta`, `IdProducto`, `Cantidad`, `PrecioUnitario`, `Subtotal`. |
| `Compra` | `Compras` | `IdCompra`, `Fecha`, `IdProveedor`, `IdEmpleado`, `Total`, `Estado` (enum `EstadoCompra`), `Observaciones`. |
| `DetalleCompra` | `DetalleCompras` | `IdDetalleCompra`, `IdCompra`, `IdProducto`, `Cantidad`, `PrecioUnitario`, `Subtotal`. |
| `EstadoVenta`, `EstadoCompra` | (enum) | Se guardan como número: Pendiente=0, Completada=1, Cancelada=2, Devuelta=3. |

Los **métodos** del diagrama están implementados en las clases: `Producto.ActualizarStock()` /
`VerificarStock()`, `Inventario.ActualizarStock()` / `VerificarStock()`, `Venta.CalcularTotal()` /
`AgregarDetalle()` / `ValidarDatos()` / `CerrarVenta()`, `DetalleVenta.CalcularSubtotal()`,
`Compra.CalcularTotal()` / `AgregarDetalle()` / `ValidarDatos()`, `DetalleCompra.CalcularSubtotal()`.
Los que dependen del usuario logueado (`login`, `cambiarPassword`, `realizarCompra`, `verHistorial`,
`registrarVenta`, `registrarCompra`, `gestionarInventario`) están en los controladores.

El **carrito** no es una tabla: se guarda en la sesión del servidor y al confirmar se convierte en una
`Venta`. Así la base de datos queda exactamente como el diagrama.

---

## 4. Prerrequisitos

- **.NET 10 SDK (LTS)**. Verificar con `dotnet --version` (debe empezar con `10.`).
- Visual Studio 2022 (17.14+) o Visual Studio Code con la extensión *C# Dev Kit*.
- Una cuenta y un proyecto creado en [Supabase](https://supabase.com).
- Una cuenta en [GitHub](https://github.com) y otra en [Render](https://render.com) para el deploy.
- (Opcional) La herramienta de EF Core para crear migraciones nuevas:

```bash
dotnet tool install --global dotnet-ef
```

---

## 5. Obtener el connection string de Supabase

1. En el panel de Supabase: **Project Settings → Database** (o el botón **Connect** arriba).
2. En *Connection string* elegir la pestaña **Session pooler** (no *Direct connection*: el pooler
   tiene salida IPv4 y evita el error de timeout por IPv6).
3. Copiar host, puerto (normalmente `5432`) y usuario (formato `postgres.xxxxxxxxxxxx`).
4. Armar la cadena así:

```
Host=aws-0-<region>.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<tu-project-ref>;Password=<TU_PASSWORD>;SSL Mode=Require;Trust Server Certificate=true
```

> Si olvidaste la contraseña de la base de datos, se puede resetear en *Project Settings → Database → Reset database password*.

---

## 6. Ejecutar en la computadora (desarrollo)

1. Clonar o copiar el proyecto y entrar a la carpeta:

```bash
cd FerreteriaApp
```

2. Poner el connection string. Hay dos opciones:
   - **Opción A (simple):** reemplazar el valor de `DefaultConnection` en `appsettings.json`.
   - **Opción B (recomendada):** crear el archivo `appsettings.Development.json` (está en `.gitignore`,
     así la contraseña no se sube a GitHub):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=aws-0-<region>.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<tu-project-ref>;Password=<TU_PASSWORD>;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

3. Restaurar paquetes y ejecutar:

```bash
dotnet restore
dotnet run
```

4. Abrir la URL que muestra la consola (por ejemplo `http://localhost:5047`).

**No hace falta correr ningún script SQL ni `dotnet ef database update`:** al arrancar, `Program.cs`
aplica las migraciones automáticamente (crea todas las tablas de Identity y de la ferretería en Supabase)
y carga los datos iniciales.

### Usuarios iniciales

| Rol | Email | Contraseña |
|---|---|---|
| Admin (empleado, cargo Administrador) | `juan@gmail.com` | `Juan1234` |
| Empleado (vendedor de ejemplo) | `vendedor@ferreteria.com` | `Vendedor123` |

El administrador se define en `appsettings.json` → sección `"Admin"` (cambiarlo antes de
entregar/desplegar). Las contraseñas deben cumplir la regla del diagrama: 8 caracteres con
mayúscula, minúscula y número. Los usuarios que se registran desde la web son **Clientes**; los
empleados los crea el administrador en **Gestión → Usuarios y empleados**.

---

## 7. Migraciones (solo si se cambian los modelos)

La migración inicial ya está en la carpeta `Migrations/`. Si más adelante se agrega o modifica un
campo en algún modelo, se genera una nueva migración y la app la aplica sola al arrancar:

```bash
dotnet ef migrations add NombreDelCambio
```

Para aplicarla a mano (opcional):

```bash
dotnet ef database update
```

---

## 8. Deploy en Render

### 8.1 Subir el proyecto a GitHub

Desde la carpeta `FerreteriaApp` (verificar antes que `appsettings.json` **no** tenga la contraseña real,
o que esté en `appsettings.Development.json` que se ignora):

```bash
git init
git add .
git commit -m "Sistema de ferretería con ASP.NET Core MVC y Supabase"
git branch -M main
git remote add origin https://github.com/<tu-usuario>/<tu-repo>.git
git push -u origin main
```

### 8.2 Crear el servicio en Render

**Opción A — Blueprint (usa `render.yaml`):**
1. En Render: **New → Blueprint** y conectar el repositorio de GitHub.
2. Render detecta `render.yaml` y crea el servicio `ferreteria-app` (plan Free, runtime Docker).
3. Cuando pida las variables de entorno, cargar:
   - `ConnectionStrings__DefaultConnection` = el connection string de Supabase (paso 5).
   - `Admin__Password` = contraseña que se quiera para el administrador (opcional).

**Opción B — Manual:**
1. **New → Web Service**, conectar el repositorio.
2. *Language / Runtime*: **Docker**. Plan: Free.
3. En **Environment Variables** agregar:

| Key | Value |
|---|---|
| `ConnectionStrings__DefaultConnection` | `Host=aws-0-...pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.xxx;Password=...;SSL Mode=Require;Trust Server Certificate=true` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Admin__Password` | (opcional) |

4. **Create Web Service**. Render construye la imagen con el `Dockerfile` y publica la app en
   `https://ferreteria-app.onrender.com` (o el nombre elegido).

> En .NET, el doble guion bajo `__` en una variable de entorno equivale a los dos puntos `:` de
> `appsettings.json`. Por eso `ConnectionStrings__DefaultConnection` reemplaza a
> `ConnectionStrings:DefaultConnection`.

Cada `git push` a `main` vuelve a desplegar automáticamente.

### 8.3 Notas sobre Render (plan Free)
- El servicio se "duerme" tras 15 minutos sin visitas; la primera visita después tarda ~30-60 s.
- El disco es **efímero**: las imágenes que suba el administrador desde la app se pierden en cada
  nuevo deploy. Las 43 imágenes iniciales sí persisten porque están en el repositorio. Para que las
  imágenes nuevas persistan, se puede pegar la **URL de una imagen** (campo *URL de imagen*) en lugar
  de subir el archivo, por ejemplo una imagen alojada en Supabase Storage.

### 8.4 Instalar la app en el celular (PWA)

La app es una **PWA** (*Progressive Web App*): el navegador la puede "instalar" como si fuera una
aplicación, sin Play Store ni App Store. Requiere que el sitio esté en **https** (Render ya lo da) o
en `localhost`.

- **Android (Chrome / Edge):** abrir la página → aparece el botón **Instalar app** en la barra (o el
  menú ⋮ → *Instalar aplicación* / *Agregar a pantalla de inicio*).
- **iPhone / iPad (Safari):** botón **Compartir** → *Agregar a pantalla de inicio*. El botón
  *Instalar app* de la barra muestra estas instrucciones.
- **PC (Chrome / Edge):** botón **Instalar app** o el ícono de instalar en la barra de direcciones.

Cómo funciona:
- `/manifest.webmanifest` (acción `Manifest` de `HomeController`): nombre de la app (sale de
  `Tienda:Nombre`), íconos, colores y `display: standalone` (se abre sin la barra del navegador).
  Al mantener apretado el ícono aparecen accesos directos a *Catálogo* y *Carrito*.
- `wwwroot/sw.js` (service worker): guarda en caché los archivos estáticos (css, js, imágenes) para
  que abra más rápido. Las páginas **siempre** se piden al servidor (así los precios, el stock y el
  carrito están al día); si no hay internet se muestra `offline.html`.
- Si se modifica `sw.js`, cambiar la constante `VERSION` (ej. `ferreteria-v2`) para que los celulares
  descarguen la versión nueva y borren la caché vieja.
- Para cambiar el ícono, reemplazar los PNG de `wwwroot/icons/` manteniendo los mismos tamaños.

---

## 9. Personalización

En `appsettings.json`:

```json
"Tienda": {
  "Nombre": "Ferretería Central",
  "Moneda": "Bs",
  "ZonaHoraria": "America/La_Paz"
}
```

- `Nombre`: se muestra en la barra, la portada y el pie de página.
- `Moneda`: símbolo con el que se muestran los precios (`Bs`, `S/`, `$`, etc.).
- `ZonaHoraria`: zona horaria para mostrar las fechas de los pedidos (se guardan en UTC).

Dirección, teléfono y horario del pie de página: `Views/Shared/_Layout.cshtml`.

---

## 10. Solución de problemas frecuentes

| Error / síntoma | Causa y solución |
|---|---|
| *Timeout* / no conecta a la base de datos | Se está usando el host de conexión directa (`db.xxx.supabase.co`), que es IPv6. Usar el connection string del **Session pooler** (paso 5). |
| `password authentication failed` | Contraseña incorrecta en el connection string. Resetearla en Supabase → *Database → Reset database password*. |
| `EntityFrameworkCore does not exist`, `IdentityDbContext<> could not be found` | Los paquetes NuGet no se restauraron. Borrar `bin/` y `obj/`, correr `dotnet restore` y `dotnet build`. |
| `dotnet ef` no se reconoce como comando | Instalar la herramienta: `dotnet tool install --global dotnet-ef` y reabrir la terminal. |
| `The view '...' was not found` | Falta el `.cshtml` correspondiente a esa acción; revisar el nombre exacto en `Views/<Controlador>/`. |
| 400 al enviar un formulario | Falta el token antifalsificación: el `<form>` debe tener `method="post"` y usar los tag helpers (`asp-action`, `asp-for`). |
| `relation "Products" already exists` u otras tablas viejas en Supabase | La base tenía la versión anterior del modelo. Borrar las tablas viejas (o crear un proyecto nuevo en Supabase) y volver a arrancar: las migraciones crean el esquema nuevo. |
| En Render el deploy falla en `dotnet restore` | Revisar que el `Dockerfile` esté en la raíz del repositorio junto al `.csproj`. |
| En Render arranca pero da error 500 | Falta la variable `ConnectionStrings__DefaultConnection` o tiene un valor incorrecto. Ver *Logs* en el panel de Render. |
| Los precios se ven con símbolo distinto | Cambiar `"Tienda:Moneda"` en `appsettings.json` (o la variable de entorno `Tienda__Moneda` en Render). |
