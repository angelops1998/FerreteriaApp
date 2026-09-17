# Instructivo completo: Sistema de Ferretería con ASP.NET Core MVC (.NET 10), Supabase y Render

> Este documento explica **todo el proyecto FerreteriaApp** desde cero, como si nunca hubiéramos
> programado en ASP.NET. El sistema implementa el **diagrama de clases UML de la ferretería**
> (Usuario/Rol con herencia Empleado y Cliente, Categoría, Producto, Proveedor, Inventario, Venta,
> DetalleVenta, FormaPago, Compra, DetalleCompra y las enumeraciones EstadoVenta y EstadoCompra). Primero se explican los conceptos, después se construye el sistema paso a
> paso (archivo por archivo, con el código completo y su explicación), y al final se muestra cómo
> ejecutarlo, probarlo y publicarlo en internet con Render. Incluye íntegro el contenido del
> `README.md` del proyecto (secciones 1, 2, 3, 4, 5, 6, 7, 8, 9 y 10 y el Anexo A).

---

## Índice

- **Parte 1 — Conceptos: qué es cada cosa**
- **Parte 2 — Preparar el entorno y crear el proyecto**
- **Parte 3 — Modelos (las clases que representan los datos)**
- **Parte 4 — Acceso a datos: DbContext y datos iniciales**
- **Parte 5 — Helpers (utilidades)**
- **Parte 6 — Program.cs: el arranque de la aplicación**
- **Parte 7 — Controladores (la lógica)**
- **Parte 8 — Vistas (las pantallas)**
- **Parte 9 — Migraciones y base de datos**
- **Parte 10 — Ejecutar y probar el sistema**
- **Parte 11 — Publicar en internet: GitHub + Render + Supabase**
- **Parte 12 — Personalización**
- **Parte 13 — Solución de problemas frecuentes**
- **Parte 14 — Preguntas típicas para defender el proyecto**
- **Anexo A — README.md del proyecto**

---

# Parte 1 — Conceptos: qué es cada cosa

## 1.1 ¿Qué vamos a construir?

El sistema implementa el **diagrama de clases UML de la ferretería** (ver Parte 3): usuarios con
roles y herencia Empleado/Cliente, categorías, proveedores, productos con inventario, ventas con
detalle y forma de pago, y compras a proveedores con detalle.

### Público (sin iniciar sesión)
- Página de inicio con categorías y productos destacados.
- Catálogo con **búsqueda**, **filtro por categoría** y **paginación**.
- Detalle de producto con productos relacionados.
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

## 1.2 ¿Qué es .NET y C#?

- **C#** (se pronuncia "si sharp") es el lenguaje de programación que usamos. Es parecido a Java:
  tiene clases, tipos, `if`, `for`, etc.
- **.NET** es la plataforma de Microsoft que ejecuta programas en C#. Es gratuita, de código abierto
  y funciona en Windows, Linux y Mac. Usamos la versión **10**, que es **LTS** (*Long Term Support*:
  con soporte por varios años).
- El comando `dotnet` (por ejemplo `dotnet run`) es la herramienta de consola de .NET: crea
  proyectos, compila, ejecuta, instala paquetes.
- Un **paquete NuGet** es una librería que se descarga de internet y se agrega al proyecto (como
  `npm` en JavaScript o `pip` en Python). Se declaran en el archivo `.csproj`.

## 1.3 ¿Qué es ASP.NET Core MVC?

**ASP.NET Core** es la parte de .NET para hacer aplicaciones web. **MVC** es la forma de organizar
el código en tres tipos de piezas:

| Pieza | Qué es | En nuestro proyecto |
|---|---|---|
| **Modelo** (Model) | Clases de C# que representan los datos: un producto, un pedido, un usuario. | `Models/Product.cs`, `Models/Order.cs`, etc. |
| **Vista** (View) | Archivos `.cshtml` que generan el HTML que ve el usuario. Mezclan HTML con C# usando el lenguaje **Razor**. | `Views/Products/Index.cshtml`, etc. |
| **Controlador** (Controller) | Clases que reciben la petición del navegador, buscan o guardan datos y eligen qué vista mostrar. | `Controllers/ProductsController.cs`, etc. |

La idea es separar responsabilidades: el controlador **decide**, el modelo **representa** y la
vista **muestra**.

## 1.4 ¿Cómo viaja una petición? (el recorrido completo)

Cuando alguien entra a `https://tu-sitio.com/Products/Details/3` pasa esto:

```
1. El navegador pide la URL  /Products/Details/3
                │
2. El ROUTING de ASP.NET la interpreta con el patrón
   "{controller}/{action}/{id?}"
   → controlador = ProductsController, acción = Details, id = 3
                │
3. Se ejecuta el método  ProductsController.Details(3)
                │
4. El controlador le pide a ENTITY FRAMEWORK el producto con Id 3
   context.Products.FirstOrDefaultAsync(p => p.Id == 3)
                │
5. Entity Framework lo traduce a SQL y lo manda a POSTGRESQL (Supabase)
   SELECT ... FROM "Products" WHERE "Id" = 3
                │
6. El controlador recibe el objeto Product y lo pasa a la vista
   return View(product);
                │
7. RAZOR procesa Views/Products/Details.cshtml y genera HTML
                │
8. El navegador recibe el HTML y lo muestra
```

Cuando se envía un formulario (por ejemplo "Agregar al carrito") es igual, pero la petición es
**POST** en lugar de **GET** y trae datos (el id del producto, la cantidad) que el controlador recibe
como parámetros del método.

## 1.5 ¿Qué es Entity Framework Core?

Es un **ORM** (*Object-Relational Mapper*): una librería que permite trabajar con la base de datos
usando objetos de C# en lugar de escribir SQL a mano.

- Cada **clase modelo** se convierte en una **tabla** (`Product` → tabla `Products`).
- Cada **propiedad** se convierte en una **columna** (`Name` → columna `Name`).
- El **DbContext** (`ApplicationDbContext`) es la clase que representa la conexión a la base de
  datos. Tiene un `DbSet<T>` por cada tabla: `context.Products`, `context.Orders`, etc.
- Las consultas se escriben con **LINQ**, por ejemplo:
  `context.Products.Where(p => p.Stock > 0).OrderBy(p => p.Name).ToListAsync()`
  y EF Core las traduce a SQL.
- Las **migraciones** son archivos generados automáticamente que describen cómo crear o modificar
  las tablas. Así nunca escribimos `CREATE TABLE` a mano.
- **Npgsql** es el "driver" que permite a EF Core hablar con **PostgreSQL** en particular.

## 1.6 ¿Qué es ASP.NET Core Identity?

Es el sistema de **usuarios, contraseñas y roles** que ya viene con ASP.NET Core. Nos da:

- Tablas de usuarios y roles (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, etc.) creadas solas.
- Contraseñas guardadas **hasheadas** (nunca en texto plano).
- Clases listas para usar: `UserManager` (crear usuarios, asignar roles) y `SignInManager`
  (iniciar y cerrar sesión).
- Autenticación por **cookie**: al iniciar sesión el navegador recibe una cookie cifrada y en cada
  petición siguiente ASP.NET sabe quién es el usuario.
- **Roles**: usamos tres, `Admin`, `Empleado` y `Cliente`. Con `[Authorize(Roles = "Admin")]` o
  `[Authorize(Roles = "Admin,Empleado")]` se protege una acción para que solo la usen esos roles.
- En este proyecto Identity se configura con **claves numéricas** (`int`) y las tablas se renombran
  (`Usuarios`, `Roles`, `UsuarioRoles`...) para que coincidan con el diagrama.

## 1.7 ¿Qué es Supabase?

Supabase es un servicio en la nube que ofrece una base de datos **PostgreSQL** gratuita (además de
otras cosas que no usamos: autenticación propia, storage, etc.). En este proyecto Supabase es
**únicamente la base de datos**: nuestra app se conecta a ella con un *connection string* y
Entity Framework crea las tablas ahí.

PostgreSQL es una base de datos relacional (tablas, filas, columnas, claves foráneas), gratuita y
muy usada.

## 1.8 ¿Qué es Render y Docker?

- **Render** es un hosting: toma nuestro código desde GitHub, lo compila y lo deja corriendo en
  internet con una URL pública (`https://ferreteria-app.onrender.com`). Tiene plan gratuito.
- **Docker** es una forma de empaquetar la aplicación con todo lo que necesita (el runtime de
  .NET, los archivos publicados) en una "imagen" que corre igual en cualquier servidor. Render lee
  nuestro archivo `Dockerfile` (una receta de cómo armar esa imagen) y lo ejecuta.

## 1.9 Tecnologías usadas

| Componente | Tecnología |
|---|---|
| Framework web | ASP.NET Core MVC — .NET 10 (LTS) |
| Acceso a datos | Entity Framework Core 10 + Npgsql |
| Base de datos | PostgreSQL (Supabase) |
| Autenticación | ASP.NET Core Identity con claves `int` (cookies, roles `Admin`, `Empleado`, `Cliente`) |
| Carrito | Sesión de ASP.NET Core (no es una tabla) |
| Front-end | Razor Views + Bootstrap 5 + Bootstrap Icons |
| Deploy | Docker en Render |

## 1.10 Glosario rápido

| Término | Significado |
|---|---|
| **Acción** | Un método público de un controlador que responde a una URL. |
| **`async` / `await`** | Forma de escribir código que espera operaciones lentas (consultas a la BD) sin bloquear el servidor. Los métodos devuelven `Task<...>`. |
| **Razor** | Sintaxis de las vistas: HTML con C# intercalado usando `@`. |
| **Tag Helper** | Atributos especiales de Razor (`asp-for`, `asp-action`, `asp-route-id`) que generan el HTML correcto de formularios y enlaces. |
| **`_Layout`** | Plantilla común de todas las páginas (barra de navegación, pie, CSS y JS). |
| **Vista parcial** | Un pedazo de vista reutilizable (por ejemplo la tarjeta de producto). Su nombre empieza con `_`. |
| **ViewModel** | Clase que existe solo para transportar datos entre un formulario y el controlador (no es una tabla). Ejemplo: `LoginViewModel`. |
| **`ViewBag`** | "Bolsa" dinámica para pasar datos extra del controlador a la vista (por ejemplo la lista de categorías). |
| **`TempData`** | Datos que sobreviven a una redirección; se usan para los mensajes "Producto creado correctamente". |
| **`ModelState`** | Resultado de la validación del formulario. `ModelState.IsValid` dice si los datos cumplen las reglas (`[Required]`, `[Range]`...). |
| **Data Annotations** | Atributos entre corchetes sobre las propiedades (`[Required]`, `[MaxLength(100)]`) que definen reglas de validación y de la tabla. |
| **Connection string** | Texto con host, usuario, contraseña y nombre de la base de datos para conectarse. |
| **Migración** | Archivo generado por EF Core que crea/modifica tablas. |
| **Seed** | Datos iniciales que se cargan automáticamente (roles, admin, productos). |
| **Rol** | Etiqueta de un usuario que define permisos (`Admin`, `User`). |
| **Transacción** | Grupo de operaciones en la BD que se ejecutan todas o ninguna. |
| **Bootstrap** | Librería de CSS que da estilo a botones, tablas, tarjetas y hace el diseño responsive. |
| **Herencia TPT** | *Table Per Type*: forma de guardar una herencia de clases en tablas: una tabla para la clase base (`Usuarios`) y una por cada clase hija (`Empleados`, `Clientes`) cuya clave es también clave foránea a la base. |
| **Sesión** | Memoria del servidor asociada al navegador por una cookie. Acá guarda el carrito. |
| **Enum** | Tipo con valores fijos con nombre (`EstadoVenta.Pendiente`). En la base se guarda como número. |
| **Token antifalsificación** | Código oculto en cada formulario que impide que otro sitio envíe formularios en nombre del usuario (`[ValidateAntiForgeryToken]`). |

---

# Parte 2 — Preparar el entorno y crear el proyecto

## 2.1 Prerrequisitos

- **.NET 10 SDK (LTS)**: descargar de <https://dotnet.microsoft.com/download>. Verificar con
  `dotnet --version` (debe empezar con `10.`).
- **Editor**: Visual Studio 2022 (17.14 o superior) con la carga de trabajo "ASP.NET y desarrollo
  web", o Visual Studio Code con la extensión *C# Dev Kit*.
- Una cuenta y un proyecto creado en **Supabase**: <https://supabase.com>.
- Una cuenta en **GitHub** (<https://github.com>) y otra en **Render** (<https://render.com>) para el
  deploy.
- (Opcional) La herramienta de EF Core para crear migraciones nuevas:

```bash
dotnet tool install --global dotnet-ef
```

## 2.2 Crear el proyecto en Supabase y obtener el connection string

1. Entrar a <https://supabase.com>, crear una cuenta y un **New project**. Elegir un nombre, una
   contraseña para la base de datos (**anotarla**, se usa en el connection string) y la región más
   cercana.
2. Esperar 1-2 minutos a que el proyecto se cree.
3. Ir a **Project Settings → Database** (o al botón **Connect** de arriba).
4. En *Connection string* elegir la pestaña **Session pooler** (no *Direct connection*: el pooler
   tiene salida IPv4 y evita el error de timeout por IPv6).
5. Copiar host, puerto (normalmente `5432`) y usuario (formato `postgres.xxxxxxxxxxxx`).
6. Armar la cadena así (reemplazar lo que está entre `< >`):

```
Host=aws-0-<region>.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<tu-project-ref>;Password=<TU_PASSWORD>;SSL Mode=Require;Trust Server Certificate=true
```

> Si olvidaste la contraseña de la base de datos, se puede resetear en *Project Settings → Database →
> Reset database password*.

> Si más adelante se despliega en un hosting *serverless* con muchas conexiones cortas (por ejemplo
> Azure Functions), se usa el **Transaction pooler** (puerto 6543). Para un sitio MVC normal, el
> Session pooler funciona perfecto y es compatible con las migraciones de EF Core.

## 2.3 Crear el proyecto ASP.NET Core MVC

Abrir una terminal en la carpeta donde se quiera el proyecto y ejecutar:

```bash
dotnet new mvc -n FerreteriaApp -f net10.0
cd FerreteriaApp
dotnet tool install --global dotnet-ef
```

`dotnet new mvc` genera un proyecto base con:

- `Program.cs`: punto de entrada.
- `Controllers/HomeController.cs` y `Views/Home/*.cshtml`: una página de ejemplo.
- `Views/Shared/_Layout.cshtml`: plantilla común.
- `wwwroot/`: archivos estáticos (CSS, JS, imágenes). Ya trae Bootstrap y jQuery en `wwwroot/lib`.
- `appsettings.json`: configuración.
- `FerreteriaApp.csproj`: el archivo del proyecto (paquetes, versión de .NET).

## 2.4 Instalar los paquetes NuGet

Solo se necesitan cinco paquetes:

```bash
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet restore
```

| Paquete | Para qué sirve |
|---|---|
| `Microsoft.EntityFrameworkCore` | El ORM (Entity Framework Core). |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | Conector de EF Core para PostgreSQL. |
| `Microsoft.EntityFrameworkCore.Design` | Necesario para generar migraciones con `dotnet ef`. |
| `Microsoft.EntityFrameworkCore.Tools` | Herramientas de EF para Visual Studio (Package Manager Console). |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Identity (usuarios y roles) guardando en EF Core. |

No se necesita el paquete "Supabase", ni JwtBearer, ni Newtonsoft.Json: la autenticación se hace
con cookies de Identity (ya incluida en ASP.NET Core) y todo el acceso a datos pasa por EF Core.

### 2.4.1 `FerreteriaApp.csproj` completo

Este es el archivo del proyecto con las versiones fijadas (si hay errores de compilación del tipo
"no existe en el namespace", reemplazar todo el contenido por este y correr `dotnet restore`):

{{FILE:FerreteriaApp.csproj:xml}}

```bash
# Si hubo que reemplazar el .csproj, desde la carpeta del proyecto:
rm -rf bin obj          # en Windows PowerShell: Remove-Item -Recurse -Force bin, obj
dotnet restore
dotnet build
```

## 2.5 `appsettings.json` y `appsettings.Development.json`

`appsettings.json` es el archivo de configuración. Se lee con `builder.Configuration["Seccion:Clave"]`.

{{FILE:appsettings.json:json}}

| Sección | Qué contiene |
|---|---|
| `ConnectionStrings:DefaultConnection` | El connection string de la base de datos (paso 2.2). |
| `Tienda` | Nombre de la ferretería, símbolo de la moneda y zona horaria para mostrar fechas. |
| `Admin` | Email, contraseña, nombre y apellido del administrador que se crea automáticamente la primera vez (es un Empleado con rol Admin). |
| `Logging` | Qué tan detallados son los mensajes en la consola. |
| `AllowedHosts` | Desde qué dominios se acepta tráfico (`*` = todos). |

**Dónde poner la contraseña real de Supabase.** Hay dos opciones:

- **Opción A (simple):** reemplazar el valor de `DefaultConnection` en `appsettings.json`.
- **Opción B (recomendada):** crear el archivo `appsettings.Development.json` con solo el connection
  string. Este archivo está en `.gitignore`, así la contraseña **no se sube a GitHub**. Cuando la
  app corre en modo *Development* (que es lo que hace `dotnet run`), los valores de este archivo
  reemplazan a los de `appsettings.json`.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=aws-0-<region>.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<tu-project-ref>;Password=<TU_PASSWORD>;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

En Render (producción) el connection string se carga como **variable de entorno** (ver Parte 11).

## 2.6 Estructura final del proyecto

```
FerreteriaApp/
├── Controllers/
│   ├── HomeController.cs          Inicio y página de error
│   ├── AccountController.cs       Login, registro (Cliente), logout, perfil, cambiar contraseña
│   ├── ProductosController.cs     Catálogo público + CRUD de productos (personal)
│   ├── CategoriasController.cs    CRUD de categorías (Admin)
│   ├── ProveedoresController.cs   CRUD de proveedores (Admin)
│   ├── FormasPagoController.cs    CRUD de formas de pago (Admin)
│   ├── CarritoController.cs       Carrito en sesión (agregar, actualizar, quitar, vaciar)
│   ├── VentasController.cs        Checkout web, mis compras, venta de mostrador, gestión de estados
│   ├── ComprasController.cs       Compras a proveedores y su ingreso al stock
│   ├── InventarioController.cs    Inventario y ajustes manuales
│   └── AdminController.cs         Panel general, usuarios y empleados
├── Models/
│   ├── Usuario.cs                 Base de Identity (int) + nombre, apellido, estado, fechaCreacion
│   ├── Rol.cs                     Rol de Identity (int) + descripción
│   ├── Empleado.cs / Cliente.cs   Heredan de Usuario (tablas Empleados y Clientes)
│   ├── Categoria.cs, Proveedor.cs, Producto.cs, Inventario.cs, FormaPago.cs
│   ├── Venta.cs (+ enum EstadoVenta), DetalleVenta.cs
│   ├── Compra.cs (+ enum EstadoCompra), DetalleCompra.cs
│   ├── CarritoItem.cs             Ítem del carrito (en sesión)
│   └── *ViewModel.cs              Formularios (login, registro, perfil, checkout, venta, compra, empleado)
├── Data/
│   ├── ApplicationDbContext.cs    Único DbContext (Identity + tablas de la ferretería)
│   └── DbInitializer.cs           Seed: roles, admin, vendedor, formas de pago, categorías, proveedores, productos, inventario
├── Helpers/
│   ├── Formato.cs                 Precios, fechas y colores de estado
│   ├── CarritoSesion.cs           Leer/guardar el carrito en la sesión
│   ├── UsuarioExtensions.cs       Id numérico del usuario logueado, EsPersonal()
│   └── SpanishIdentityErrorDescriber.cs   Mensajes de Identity en español
├── Migrations/                    Migraciones de EF Core (ya generadas)
├── Views/                         Vistas Razor (Home, Account, Productos, Categorias, Proveedores, FormasPago, Carrito, Ventas, Compras, Inventario, Admin, Shared)
├── wwwroot/
│   ├── css/site.css               Estilos propios
│   └── images/productos/          Imágenes de los productos
├── appsettings.json               Configuración (connection string, nombre de la tienda, admin)
├── Program.cs
├── Dockerfile                     Para el deploy en Render
└── render.yaml                    Blueprint de Render
```

**CRUD** significa *Create, Read, Update, Delete*: crear, leer, actualizar y borrar registros.

---

# Parte 3 — Modelos (las clases que representan los datos)

## 3.0 Del diagrama de clases a las tablas

El punto de partida es el **diagrama de clases UML** de la ferretería. Cada clase del diagrama se
convierte en una clase de C# en `Models/` y, gracias a Entity Framework, en una tabla de PostgreSQL:

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

Decisiones de diseño que conviene poder explicar:

- **Identity para Usuario y Rol.** En lugar de programar a mano el login y el guardado de contraseñas
  (inseguro), `Usuario` hereda de `IdentityUser<int>` y `Rol` de `IdentityRole<int>`. Se renombran
  las tablas de Identity (`AspNetUsers` → `Usuarios`, etc.) para respetar el diagrama.
- **Herencia Empleado/Cliente con TPT.** La flecha de herencia del diagrama se traduce con
  *Table Per Type*: `Usuarios` tiene los datos comunes y `Empleados` / `Clientes` los propios, unidas
  por la clave. En C# se escribe simplemente `class Empleado : Usuario`.
- **Relaciones.** *Venta "se paga con" FormaPago* se implementa como una forma de pago por venta
  (`IdFormaPago`); *DetalleVenta "es de" Producto* mediante `IdProducto`; *Producto "tiene"
  Inventario* con un registro por producto.
- **El carrito no es una tabla.** No está en el diagrama, así que se guarda en la **sesión** del
  servidor y solo al confirmar se convierte en `Venta` + `DetalleVenta`.
- **Métodos del diagrama.** Están implementados como métodos de las clases (`Producto.ActualizarStock`,
  `Venta.CalcularTotal`, `Venta.AgregarDetalle`, `Compra.AgregarDetalle`,
  `DetalleVenta.CalcularSubtotal`, `Inventario.VerificarStock`...). Los que dependen del usuario
  logueado (`login`, `realizarCompra`, `registrarVenta`, `gestionarInventario`...) viven en los
  controladores.

## 3.1 Data Annotations: las reglas de validación

Antes de ver cada archivo, conviene entender los atributos entre corchetes que van sobre las
propiedades:

| Atributo | Qué hace |
|---|---|
| `[Key]` | Marca la propiedad como clave primaria (`IdProducto`). EF la crea autoincremental. |
| `[Required]` | El campo es obligatorio. Si el formulario viene vacío, `ModelState.IsValid` es `false`. |
| `[MaxLength(100)]` | Largo máximo del texto (también define el tamaño de la columna). |
| `[Range(0.01, 999999.99)]` | Valor mínimo y máximo permitidos. `[Range(1, int.MaxValue)]` sirve para un combo obligatorio (0 = sin elegir). |
| `[RegularExpression(@"^\d{8,15}$")]` | El texto debe cumplir una expresión regular (acá: solo dígitos, entre 8 y 15). |
| `[Display(Name = "Nombre")]` | Texto de la etiqueta que se muestra en los formularios (`<label asp-for=...>`). |
| `[DataType(DataType.Password)]` | Indica que el campo se muestre como contraseña (`type="password"`). |
| `[EmailAddress]` | Valida que el texto tenga formato de email. |
| `[Compare("Password")]` | Valida que dos campos sean iguales (contraseña y confirmación). |
| `[StringLength(100, MinimumLength = 8)]` | Largo mínimo y máximo. |
| `[NotMapped]` | La propiedad **no** se guarda en la base de datos (existe solo en memoria). |

Así se cumplen las **validaciones del diagrama**: email con formato, teléfono de 8 a 15 dígitos,
nombre de producto obligatorio de máximo 100 caracteres, precios > 0, stock ≥ 0, categoría y
proveedor obligatorios, cantidad y precio unitario > 0. La regla de contraseña (8 caracteres con
mayúscula, minúscula y número) la aplica Identity (Parte 6), y "no vender sin stock" / "no dejar stock
negativo" las aplica el código de las clases y los controladores.

Otras cosas que aparecen en los modelos:

- `string?` (con `?`) significa que la propiedad **puede ser null** (opcional).
- `DateTime.UtcNow` guarda la fecha en UTC. Después se convierte a la zona horaria de la tienda al
  mostrarla (`Helpers/Formato.cs`).
- Una propiedad de tipo otra clase (`public Categoria? Categoria`) junto con su Id (`IdCategoria`)
  es una **propiedad de navegación**: representa una clave foránea. `ICollection<Producto> Productos`
  es el lado "muchos" de la relación.

## 3.2 `Models/Usuario.cs` y `Models/Rol.cs` — usuario base y rol

{{FILE:Models/Usuario.cs:csharp}}

{{FILE:Models/Rol.cs:csharp}}

`Estado = false` desactiva la cuenta: el `AccountController` no la deja iniciar sesión.

## 3.3 `Models/Empleado.cs` y `Models/Cliente.cs` — la herencia

{{FILE:Models/Empleado.cs:csharp}}

{{FILE:Models/Cliente.cs:csharp}}

`class Empleado : Usuario` es toda la herencia en C#. Qué tabla usa cada clase se configura en el
`DbContext` (Parte 4).

## 3.4 `Models/Categoria.cs` y `Models/Proveedor.cs`

{{FILE:Models/Categoria.cs:csharp}}

{{FILE:Models/Proveedor.cs:csharp}}

## 3.5 `Models/Producto.cs` — el producto y sus métodos

{{FILE:Models/Producto.cs:csharp}}

- `PrecioVenta` y `PrecioCompra` son `decimal` (no `double`): es el tipo correcto para dinero.
- `ImagenUrl` guarda la **ruta** de la imagen (`/images/productos/martillo-generico.png`), no la
  imagen en sí. `ImagenArchivo` (`[NotMapped]`) solo recibe el archivo subido en el formulario.
- `ActualizarStock(cantidad)` suma o resta unidades **y** mantiene sincronizado el `Inventario`;
  lanza una excepción si el stock quedaría negativo (regla del diagrama).
- `VerificarStock()` devuelve `true` si el stock está por encima del mínimo.

## 3.6 `Models/Inventario.cs`

{{FILE:Models/Inventario.cs:csharp}}

## 3.7 `Models/FormaPago.cs`

{{FILE:Models/FormaPago.cs:csharp}}

## 3.8 `Models/Venta.cs` y `Models/DetalleVenta.cs` — la venta

{{FILE:Models/Venta.cs:csharp}}

- `EstadoVenta` es un **enum** con los cuatro estados del diagrama.
- `IdEmpleado` es `int?` (opcional): una compra hecha desde la web queda sin empleado hasta que
  alguien del personal la completa (`CerrarVenta`).
- `AgregarDetalle(producto, cantidad)` crea la línea con el **precio de venta actual** y valida el
  stock. `CalcularTotal()` suma los subtotales. `DescuentaStock()` dice si en ese estado el stock
  está descontado (Pendiente/Completada) o devuelto (Cancelada/Devuelta).

{{FILE:Models/DetalleVenta.cs:csharp}}

Se copian `PrecioUnitario` y `Subtotal` **en el momento de la venta**. Si después cambia el precio
del producto, la venta vieja sigue mostrando lo que se cobró realmente.

## 3.9 `Models/Compra.cs` y `Models/DetalleCompra.cs` — la compra a proveedor

{{FILE:Models/Compra.cs:csharp}}

{{FILE:Models/DetalleCompra.cs:csharp}}

## 3.10 ViewModels: clases solo para formularios

Un **ViewModel** no es una tabla: agrupa exactamente los campos de un formulario con sus validaciones.

### `LoginViewModel.cs` y `RegistroViewModel.cs`

{{FILE:Models/LoginViewModel.cs:csharp}}

{{FILE:Models/RegistroViewModel.cs:csharp}}

### `PerfilViewModel.cs` y `CambiarPasswordViewModel.cs`

{{FILE:Models/PerfilViewModel.cs:csharp}}

{{FILE:Models/CambiarPasswordViewModel.cs:csharp}}

### `CarritoItem.cs` y `CheckoutViewModel.cs`

{{FILE:Models/CarritoItem.cs:csharp}}

{{FILE:Models/CheckoutViewModel.cs:csharp}}

### `RegistrarVentaViewModel.cs` y `RegistrarCompraViewModel.cs`

Formularios con **varias filas** de productos: `Items` es una lista y cada fila llega como
`Items[0].IdProducto`, `Items[0].Cantidad`, `Items[1].IdProducto`... ASP.NET arma la lista sola.

{{FILE:Models/RegistrarVentaViewModel.cs:csharp}}

{{FILE:Models/RegistrarCompraViewModel.cs:csharp}}

### `EmpleadoViewModel.cs` y `ErrorViewModel.cs`

{{FILE:Models/EmpleadoViewModel.cs:csharp}}

{{FILE:Models/ErrorViewModel.cs:csharp}}

## 3.11 Cómo quedan las tablas (relaciones)

```
              Roles ──N:M (UsuarioRoles)── Usuarios
                                          ┌───┴────┐  (herencia TPT)
                                      Empleados  Clientes
                                          │          │
        Proveedores ──1:N── Compras ──N:1─┘          │
             │                 │                     │
             │             DetalleCompras            │
             │                 │ N:1                 │
        (1:N)│           ┌─────┴──────┐              │
        Categorias ─1:N─ Productos ─1:N─ Inventarios │
                            │ N:1                    │
                       DetalleVentas ──N:1── Ventas ─┘ (N:1)
                                               │ N:1
                                           FormasPago
```

- Un usuario es Empleado **o** Cliente; su rol está en `UsuarioRoles`.
- Una categoría y un proveedor tienen muchos productos (obligatorios: no se pueden borrar si tienen
  productos, se desactivan).
- Un producto tiene su registro de inventario (se borra en cascada con el producto).
- Una venta pertenece a un cliente, opcionalmente a un empleado y a una forma de pago; tiene muchas
  líneas (`DetalleVentas`, en cascada).
- Una compra pertenece a un proveedor y al empleado que la registró; tiene muchas líneas.
- **No se puede borrar un producto** que aparezca en una venta o compra (se desactiva).

---

# Parte 4 — Acceso a datos: DbContext y datos iniciales

## 4.1 `Data/ApplicationDbContext.cs` — la conexión a la base de datos

{{FILE:Data/ApplicationDbContext.cs:csharp}}

Explicación por bloques:

- `IdentityDbContext<Usuario, Rol, int>`: el mismo contexto maneja las tablas de Identity (con
  nuestras clases y clave `int`) **y** las de la ferretería.
- **Tablas de Identity renombradas**: `ToTable("Usuarios")`, `HasColumnName("IdUsuario")`, etc.
  Identity sigue funcionando igual; solo cambian los nombres en PostgreSQL.
- **Herencia TPT**: `Entity<Empleado>().ToTable("Empleados", t => t.Property(e => e.Id).HasColumnName("IdEmpleado"))`
  le dice a EF que los empleados van en su propia tabla, con la clave llamada `IdEmpleado`.
  Al haber tablas distintas para la base y las hijas, EF aplica automáticamente *Table Per Type*.
- `HasPrecision(18, 2)`: los importes se guardan con 2 decimales.
- **Relaciones y borrado**:
  - `Restrict`: la base de datos **impide** borrar el registro padre si tiene hijos (categoría o
    proveedor con productos, producto vendido, cliente con ventas...). El código lo maneja desactivando.
  - `Cascade`: al borrar el padre se borran los hijos (venta → detalles, producto → inventario).

## 4.2 `Data/DbInitializer.cs` — datos iniciales (seed)

Se ejecuta cada vez que arranca la app, pero solo crea lo que falta:

1. Los roles `Admin`, `Empleado` y `Cliente`.
2. El administrador (un `Empleado` con cargo "Administrador" y rol `Admin`), con el email y
   contraseña de `appsettings.json` → `Admin`.
3. Un vendedor de ejemplo (`vendedor@ferreteria.com` / `Vendedor123`) con rol `Empleado`.
4. Las formas de pago (Efectivo, Tarjeta, Transferencia, QR).
5. Las 8 categorías, los 6 proveedores, los 43 productos y un registro de **inventario** por producto,
   **solo si la tabla de productos está vacía**.

{{FILE:Data/DbInitializer.cs:csharp}}

Detalles:

- `userManager.CreateAsync(admin, adminPassword)` crea el usuario **hasheando** la contraseña, y como
  `admin` es un `Empleado`, EF inserta en `Usuarios` y en `Empleados` (herencia TPT).
- `P(...)` es una **función local** que arma cada producto. El precio de compra se estima como el
  70% del de venta.
- Cada producto se asigna a un proveedor según su marca (Bosch, DeWalt, Stanley, Daewoo, genéricos o
  importadora de herramientas profesionales).

---

# Parte 5 — Helpers (utilidades)

## 5.1 `Helpers/Formato.cs` — precios, fechas y colores

{{FILE:Helpers/Formato.cs:csharp}}

- `Precio(decimal)`: siempre usa punto decimal y separador de miles, con el símbolo configurado.
- `Fecha(DateTime)`: convierte la fecha UTC guardada en la base a la zona horaria de la tienda.
- `EstadoBadge(...)`: color de la etiqueta Bootstrap para cada estado de venta o de compra.

## 5.2 `Helpers/CarritoSesion.cs` — el carrito en la sesión

{{FILE:Helpers/CarritoSesion.cs:csharp}}

La sesión solo guarda texto, así que la lista de `CarritoItem` se convierte a JSON con
`JsonSerializer` al guardar y se vuelve a convertir al leer.

## 5.3 `Helpers/UsuarioExtensions.cs` — id del usuario logueado

{{FILE:Helpers/UsuarioExtensions.cs:csharp}}

Identity guarda el id del usuario en la cookie como texto; `GetUsuarioId()` lo devuelve como `int`
para usarlo en `IdCliente` / `IdEmpleado`. `EsPersonal()` es `true` para Admin y Empleado.

## 5.4 `Helpers/SpanishIdentityErrorDescriber.cs` — mensajes de Identity en español

{{FILE:Helpers/SpanishIdentityErrorDescriber.cs:csharp}}

---

# Parte 6 — `Program.cs`: el arranque de la aplicación

`Program.cs` es el primer archivo que se ejecuta. Tiene dos mitades: **antes** de `builder.Build()`
se registran los servicios; **después** se arma la "tubería" de middlewares (qué pasa con cada
petición, en orden).

{{FILE:Program.cs:csharp}}

Explicación por bloques:

1. **Cultura invariante**: obliga a que los números se lean y escriban con punto decimal (`99.50`).
2. **Puerto**: Render indica por la variable `PORT` en qué puerto escuchar.
3. **`AddDbContext`**: registra el `ApplicationDbContext` con PostgreSQL (`UseNpgsql`).
4. **`AddIdentity<Usuario, Rol>`**: activa usuarios y roles con nuestras clases. Las reglas de
   contraseña son las del diagrama: **8 caracteres mínimo, con mayúscula, minúscula y número**
   (sin obligar símbolos); email único. `AddErrorDescriber` pone los mensajes en español.
5. **`ConfigureApplicationCookie`**: rutas del login, logout y acceso denegado; sesión de 60 minutos.
6. **`AddSession`**: activa la sesión del servidor, donde vive el carrito (2 horas de inactividad).
7. **`AddControllersWithViews`** con los mensajes genéricos del model binding en español (por
   ejemplo cuando un combo llega vacío o un número no es válido).
8. **`UseForwardedHeaders`**: para que detrás del proxy de Render la app sepa que la petición fue HTTPS.
9. **Pipeline** (el orden importa): errores/HSTS solo en producción, `UseHttpsRedirection`,
   `UseStaticFiles` (sirve `wwwroot`, incluidas las imágenes subidas), `UseRouting`, `UseSession`,
   `UseAuthentication`, `UseAuthorization` y la ruta `{controller=Home}/{action=Index}/{id?}`.
10. **Migraciones y seed al arrancar**: `MigrateAsync()` crea o actualiza las tablas y
    `DbInitializer.SeedAsync` carga los datos iniciales.

---

# Parte 7 — Controladores (la lógica)

## 7.0 Anatomía de un controlador

```csharp
[Authorize(Roles = "Admin,Empleado")]         // ← toda la clase requiere uno de estos roles
public class ComprasController(ApplicationDbContext context) : Controller   // ← constructor primario
{
    public async Task<IActionResult> Index()  // ← una ACCIÓN: responde a /Compras o /Compras/Index
    {
        var compras = await context.Compras.ToListAsync();   // ← consulta a la BD
        return View(compras);                 // ← renderiza Views/Compras/Index.cshtml con esos datos
    }

    [HttpPost]                                // ← solo responde a formularios enviados (POST)
    [ValidateAntiForgeryToken]                // ← verifica el token oculto del formulario
    public async Task<IActionResult> Registrar(RegistrarCompraViewModel model)   // ← los campos del form
    {
        if (!ModelState.IsValid) return View(model);   // ← si falla la validación, vuelve al form
        var compra = new Compra { /* ... */ };
        context.Compras.Add(compra);
        await context.SaveChangesAsync();     // ← recién acá se escribe en la BD
        TempData["Exito"] = "Compra registrada.";        // ← mensaje que se muestra en la próxima página
        return RedirectToAction(nameof(Details), new { id = compra.IdCompra });
    }
}
```

Conceptos:

- **Constructor primario**: `(ApplicationDbContext context)` recibe el DbContext por
  **inyección de dependencias** (ASP.NET lo crea y lo pasa solo).
- **`IActionResult`**: lo que devuelve una acción: `View(...)`, `RedirectToAction(...)`,
  `NotFound()` (404), `Forbid()` (403), `Challenge()` (mandar al login).
- **GET vs POST**: el método sin atributo muestra la página; el que tiene `[HttpPost]` procesa el
  formulario. Los dos se llaman igual, C# los distingue por los parámetros.
- **Model binding**: ASP.NET arma el objeto con los campos del formulario que tengan el mismo nombre.
- **`AsNoTracking()`**: consultas de solo lectura más rápidas. **`Include(...)`**: carga la entidad
  relacionada (un JOIN); sin esto `venta.Cliente` sería `null`.
- **`TempData["Exito"]` / `["Error"]` / `["Info"]`**: mensajes que `_Alertas.cshtml` muestra como
  cartel verde, rojo o celeste.
- **`User.GetUsuarioId()`**: id numérico del usuario logueado (helper de la Parte 5).
- **Transacción** (`BeginTransactionAsync` ... `CommitAsync`): varias escrituras se guardan juntas o
  ninguna. Si una excepción sale del `using` sin commit, se deshace todo.

## 7.1 `Controllers/HomeController.cs` — inicio

{{FILE:Controllers/HomeController.cs:csharp}}

## 7.2 `Controllers/AccountController.cs` — login, registro, perfil, contraseña

{{FILE:Controllers/AccountController.cs:csharp}}

- **Login** (`Usuario.login`): si el usuario existe pero `Estado = false`, no lo deja entrar.
  `PasswordSignInAsync` compara la contraseña con el hash y crea la cookie. El personal va al panel;
  los clientes al catálogo.
- **Register**: el registro público crea un **`Cliente`** (tipo Regular) con rol `Cliente`.
  Identity valida la contraseña con las reglas de `Program.cs`.
- **Perfil** (`Cliente.actualizarDatos`): el `switch (usuario)` con *pattern matching* distingue si
  es `Cliente` o `Empleado` para guardar teléfono y dirección en la tabla que corresponde.
- **CambiarPassword** (`Usuario.cambiarPassword`): `ChangePasswordAsync` verifica la actual y aplica
  las reglas a la nueva.

## 7.3 `Controllers/ProductosController.cs` — catálogo y CRUD de productos

{{FILE:Controllers/ProductosController.cs:csharp}}

- **Index (catálogo)**: solo productos activos de categorías activas; búsqueda con `ILike`
  (PostgreSQL, sin distinguir mayúsculas), filtro por categoría y paginación con `Skip`/`Take`.
- **Create** (Admin y Empleado): valida la imagen, la guarda con un nombre único y crea el producto
  **junto con su registro de `Inventario`**.
- **Edit**: copia solo los campos editables sobre el original. Si el stock se cambió a mano, llama a
  `ActualizarStock` para que el inventario quede sincronizado con fecha.
- **Delete** (solo Admin): si el producto aparece en ventas o compras se **desactiva**; si no, se borra
  y su inventario se borra en cascada.

## 7.4 `Controllers/CategoriasController.cs`, `ProveedoresController.cs` y `FormasPagoController.cs`

Los tres son CRUD de administrador con el mismo patrón: lista, crear, editar, eliminar. Como estas
entidades son obligatorias en otras tablas (un producto necesita categoría y proveedor; una venta
necesita forma de pago), si están en uso **se desactivan** en lugar de borrarse.

{{FILE:Controllers/CategoriasController.cs:csharp}}

{{FILE:Controllers/ProveedoresController.cs:csharp}}

{{FILE:Controllers/FormasPagoController.cs:csharp}}

## 7.5 `Controllers/CarritoController.cs` — carrito en sesión

{{FILE:Controllers/CarritoController.cs:csharp}}

- No tiene `[Authorize]`: cualquier visitante puede armar su carrito. Al ir a "Finalizar compra"
  se le pide iniciar sesión y el carrito se conserva (vive en la sesión, no en el usuario).
- **Agregar**: si el producto ya está, suma la cantidad; nunca supera el stock (`Math.Min`).
- **Actualizar / Quitar / Vaciar**: modifican la lista y la vuelven a guardar en la sesión.

## 7.6 `Controllers/VentasController.cs` — ventas web y de mostrador

{{FILE:Controllers/VentasController.cs:csharp}}

- **Checkout (POST)** (`Cliente.realizarCompra`), la parte más importante:
  1. Lee el carrito de la sesión y el cliente logueado.
  2. Abre una **transacción**.
  3. Para cada ítem, `venta.AgregarDetalle(producto, cantidad)` crea la línea con el precio actual y
     `producto.ActualizarStock(-cantidad)` descuenta stock e inventario. Si no alcanza, la excepción
     se convierte en un error del formulario ("no permitir vender si no hay stock").
  4. `CalcularTotal()`, se guarda la venta en estado *Pendiente* y se hace **commit**. Recién ahí se
     vacía el carrito.
- **MisCompras / Details** (`Cliente.verHistorial`): un cliente solo ve sus propias ventas
  (`Forbid()` si intenta ver otra); el personal las ve todas.
- **Cancelar**: el cliente puede cancelar solo si está *Pendiente*; se repone el stock.
- **Manage / CambiarEstado** (personal): `CambiarEstado` repone o vuelve a descontar el stock según
  se pase de un estado que lo descuenta (Pendiente/Completada) a uno que lo devuelve
  (Cancelada/Devuelta) o viceversa. Al pasar a *Completada* se llama a `CerrarVenta(idEmpleado)`.
- **Registrar** (`Empleado.registrarVenta`): venta de mostrador. Se eligen cliente, forma de pago y
  filas de productos; se agrupan filas repetidas, se valida el stock y la venta queda *Completada* a
  nombre del empleado logueado.

## 7.7 `Controllers/ComprasController.cs` — compras a proveedores

{{FILE:Controllers/ComprasController.cs:csharp}}

- **Registrar** (`Empleado.registrarCompra`): crea la compra *Pendiente* con sus líneas al precio
  unitario indicado. **Todavía no toca el stock**: la mercadería no llegó.
- **CambiarEstado**: al pasar a *Completada* ingresa la mercadería (`ActualizarStock(+cantidad)`) y
  actualiza el `PrecioCompra` del producto con el último precio pagado. Si se cancela una compra
  ya completada, se descuenta; si el stock quedaría negativo, `ActualizarStock` lanza la excepción y
  la transacción se deshace ("no permitir compra con stock negativo").

## 7.8 `Controllers/InventarioController.cs` — inventario

{{FILE:Controllers/InventarioController.cs:csharp}}

`Empleado.gestionarInventario`: lista el inventario (con filtro "bajo mínimo" usando
`Inventario.VerificarStock`) y permite un **ajuste manual** tras un recuento físico, manteniendo
`Producto.Stock` e `Inventario.StockActual` iguales.

## 7.9 `Controllers/AdminController.cs` — panel y usuarios

{{FILE:Controllers/AdminController.cs:csharp}}

- **Index (panel)**: totales, ventas completadas, compras completadas, stock bajo (desde
  `Inventarios`), últimas ventas y más vendidos (con `GroupBy`).
- **Usuarios** (solo Admin): lista todos con su tipo (Empleado/Cliente) y rol.
- **ToggleEstado**: activa/desactiva (`Usuario.estado`). Nadie puede desactivarse a sí mismo.
- **CambiarRol**: alterna Empleado ↔ Admin. Solo para empleados: un cliente nunca puede ser admin.
- **CambiarTipoCliente**: Regular ↔ Mayorista.
- **CrearEmpleado / EditarEmpleado**: alta y edición de empleados (con contraseña y rol). En la
  edición, la contraseña se cambia solo si se escribe una nueva (`ResetPasswordAsync` con token).

---

# Parte 8 — Vistas (las pantallas)

## 8.0 Cómo funciona una vista Razor

Una vista es un archivo `.cshtml` en `Views/<Controlador>/<Accion>.cshtml`. Es HTML normal donde se
puede intercalar C# con `@`:

```cshtml
@model List<Producto>                @* tipo de dato que recibe la vista (lo que pasó el controlador con View(...)) *@
@{
    ViewData["Title"] = "Catálogo";  @* bloque de código C#; Title lo usa el _Layout para el <title> *@
}

<h2>@ViewData["Title"]</h2>          @* imprimir un valor *@

@foreach (var p in Model)            @* Model es el objeto recibido; acá una lista de productos *@
{
    <p>@p.Nombre — @Formato.Precio(p.PrecioVenta)</p>
}

@if (User.IsInRole("Admin"))         @* condicional *@
{
    <a asp-action="Create">Crear</a>
}
```

**Tag Helpers** (atributos que empiezan con `asp-`):

| Tag helper | Qué genera |
|---|---|
| `<form asp-action="Login" method="post">` | `<form action="/Account/Login" method="post">` **más** el token antifalsificación oculto. |
| `<a asp-controller="Productos" asp-action="Details" asp-route-id="@p.IdProducto">` | `<a href="/Productos/Details/3">` |
| `<label asp-for="Email">` | `<label for="Email">Email</label>` (usa el `[Display]` del modelo). |
| `<input asp-for="Email">` | `<input type="email" id="Email" name="Email" value="...">` con los atributos de validación. |
| `<span asp-validation-for="Email">` | Muestra el mensaje de error de ese campo. |
| `<div asp-validation-summary="ModelOnly">` | Muestra los errores generales (`"All"` muestra también los de cada campo). |
| `<select asp-for="IdCategoria" asp-items="ViewBag.Categorias">` | Un combo con las opciones de una lista. |
| `<partial name="_ProductoCard" model="p" />` | Inserta una vista parcial pasándole un modelo. |

Archivos especiales de la carpeta `Views/`:

- `_ViewImports.cshtml`: `using` y tag helpers disponibles en **todas** las vistas.
- `_ViewStart.cshtml`: dice que todas las vistas usan `_Layout`.
- `Shared/_Layout.cshtml`: la plantilla. `@RenderBody()` es donde se inserta cada vista.
- `Shared/_ValidationScriptsPartial.cshtml`: scripts de jQuery que validan el formulario en el
  navegador antes de enviarlo. Se incluye con `@section Scripts { ... }`.

**Bootstrap**: las clases como `btn btn-warning`, `card`, `table table-hover`, `row`, `col-md-4`
vienen de Bootstrap 5. Los íconos (`<i class="bi bi-cart3">`) son de Bootstrap Icons.

## 8.1 `Views/_ViewImports.cshtml` y `Views/_ViewStart.cshtml`

{{FILE:Views/_ViewImports.cshtml:cshtml}}

{{FILE:Views/_ViewStart.cshtml:cshtml}}

## 8.2 `Views/Shared/_Layout.cshtml` — la plantilla común

{{FILE:Views/Shared/_Layout.cshtml:cshtml}}

- Con `User.IsInRole(...)` se arma el menú según el rol: **Gestión** para el personal (con las opciones
  de Admin al final), **Mis compras** y el carrito para clientes y visitantes.
- El numerito del carrito sale de la sesión (`CarritoSesion.Cantidad`), sin consultar la base.
- El "Cerrar sesión" es un `<form method="post">` porque `Logout` es `[HttpPost]`.

## 8.3 `Views/Shared/_Alertas.cshtml`, `_ProductoCard.cshtml` y `Error.cshtml`

{{FILE:Views/Shared/_Alertas.cshtml:cshtml}}

{{FILE:Views/Shared/_ProductoCard.cshtml:cshtml}}

{{FILE:Views/Shared/Error.cshtml:cshtml}}

## 8.4 `Views/Home/Index.cshtml` — portada

{{FILE:Views/Home/Index.cshtml:cshtml}}

## 8.5 Vistas de cuenta (`Views/Account/`)

{{FILE:Views/Account/Login.cshtml:cshtml}}

{{FILE:Views/Account/Register.cshtml:cshtml}}

{{FILE:Views/Account/Perfil.cshtml:cshtml}}

{{FILE:Views/Account/CambiarPassword.cshtml:cshtml}}

{{FILE:Views/Account/AccessDenied.cshtml:cshtml}}

## 8.6 Vistas de productos (`Views/Productos/`)

### `Index.cshtml` — catálogo con filtros y paginación

{{FILE:Views/Productos/Index.cshtml:cshtml}}

### `Details.cshtml`

{{FILE:Views/Productos/Details.cshtml:cshtml}}

### `Manage.cshtml` — tabla de administración

{{FILE:Views/Productos/Manage.cshtml:cshtml}}

### `_Form.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Delete.cshtml`

Para no repetir el formulario dos veces, los campos están en una vista parcial. El
`<input type="file">` requiere que el `<form>` tenga `enctype="multipart/form-data"`.

{{FILE:Views/Productos/_Form.cshtml:cshtml}}

{{FILE:Views/Productos/Create.cshtml:cshtml}}

{{FILE:Views/Productos/Edit.cshtml:cshtml}}

{{FILE:Views/Productos/Delete.cshtml:cshtml}}

## 8.7 Catálogos de administración: `Views/Categorias/`, `Views/Proveedores/`, `Views/FormasPago/`

Las tres carpetas siguen el mismo patrón (lista, `_Form` compartido, crear, editar, eliminar). Se
muestra completa la de categorías; proveedores y formas de pago son iguales cambiando los campos.

{{FILE:Views/Categorias/Index.cshtml:cshtml}}

{{FILE:Views/Categorias/_Form.cshtml:cshtml}}

{{FILE:Views/Categorias/Create.cshtml:cshtml}}

{{FILE:Views/Categorias/Edit.cshtml:cshtml}}

{{FILE:Views/Categorias/Delete.cshtml:cshtml}}

Proveedores (`_Form.cshtml`, con RUC/NIT, teléfono y dirección):

{{FILE:Views/Proveedores/_Form.cshtml:cshtml}}

{{FILE:Views/Proveedores/Index.cshtml:cshtml}}

Formas de pago (`_Form.cshtml` e `Index.cshtml`):

{{FILE:Views/FormasPago/_Form.cshtml:cshtml}}

{{FILE:Views/FormasPago/Index.cshtml:cshtml}}

## 8.8 `Views/Carrito/Index.cshtml` — carrito

{{FILE:Views/Carrito/Index.cshtml:cshtml}}

## 8.9 Vistas de ventas (`Views/Ventas/`)

### `Checkout.cshtml` — finalizar compra (cliente)

{{FILE:Views/Ventas/Checkout.cshtml:cshtml}}

### `MisCompras.cshtml`

{{FILE:Views/Ventas/MisCompras.cshtml:cshtml}}

### `Details.cshtml` — la misma vista para cliente y personal

{{FILE:Views/Ventas/Details.cshtml:cshtml}}

### `Manage.cshtml` — gestión de ventas (personal)

{{FILE:Views/Ventas/Manage.cshtml:cshtml}}

### `Registrar.cshtml` — venta de mostrador con filas dinámicas

El JavaScript del final agrega y quita filas, **renumera** los nombres (`Items[0]`, `Items[1]`...)
para que ASP.NET arme la lista, y calcula subtotales y total en vivo con los `data-precio` y
`data-stock` de cada opción.

{{FILE:Views/Ventas/Registrar.cshtml:cshtml}}

## 8.10 Vistas de compras (`Views/Compras/`)

{{FILE:Views/Compras/Index.cshtml:cshtml}}

{{FILE:Views/Compras/Details.cshtml:cshtml}}

{{FILE:Views/Compras/Registrar.cshtml:cshtml}}

## 8.11 Vistas de inventario (`Views/Inventario/`)

{{FILE:Views/Inventario/Index.cshtml:cshtml}}

{{FILE:Views/Inventario/Ajustar.cshtml:cshtml}}

## 8.12 Vistas de administración (`Views/Admin/`)

{{FILE:Views/Admin/Index.cshtml:cshtml}}

{{FILE:Views/Admin/Usuarios.cshtml:cshtml}}

{{FILE:Views/Admin/_EmpleadoForm.cshtml:cshtml}}

{{FILE:Views/Admin/CrearEmpleado.cshtml:cshtml}}

{{FILE:Views/Admin/EditarEmpleado.cshtml:cshtml}}

## 8.13 `wwwroot/css/site.css` — estilos propios

{{FILE:wwwroot/css/site.css:css}}

---

# Parte 9 — Migraciones y base de datos

## 9.1 Qué es una migración y cómo se generó

Una migración es una clase de C# generada automáticamente que describe los cambios a hacer en la
base de datos (crear tablas, columnas, índices, claves foráneas). EF Core compara los modelos con la
última "foto" de la base (`ApplicationDbContextModelSnapshot.cs`) y genera la diferencia.

La migración inicial del proyecto se generó con:

```bash
dotnet ef migrations add InitialCreate
```

Eso creó la carpeta `Migrations/` con tres archivos: `..._InitialCreate.cs` (el método `Up()` crea
todas las tablas y `Down()` las borra), su `.Designer.cs` y el snapshot.

**No hace falta ningún script SQL manual**: EF Core crea automáticamente TODAS las tablas (las de
Identity, la herencia Empleado/Cliente y las de la ferretería) a partir del modelo.

## 9.2 Cómo se aplica

1. **Automática al arrancar** (`Program.cs` → `context.Database.MigrateAsync()`): cada vez que la
   app arranca aplica las migraciones que falten. Ideal para Render.
2. **Manual**: `dotnet ef database update` desde la carpeta del proyecto.

EF lleva el registro de qué migraciones ya aplicó en la tabla `__EFMigrationsHistory`.

## 9.3 Tablas que se crean en Supabase

| Tabla | Origen | Contenido |
|---|---|---|
| `Usuarios` | Identity (`Usuario`) | Todos los usuarios: email, hash de contraseña, nombre, apellido, estado, fecha de creación |
| `Roles` | Identity (`Rol`) | `Admin`, `Empleado`, `Cliente` con descripción |
| `UsuarioRoles` | Identity | Qué usuario tiene qué rol |
| `UsuarioClaims`, `UsuarioLogins`, `UsuarioTokens`, `RolClaims` | Identity | Tablas auxiliares (no las usamos directamente) |
| `Empleados` | Herencia TPT | `IdEmpleado` (= `IdUsuario`), cargo, teléfono, dirección |
| `Clientes` | Herencia TPT | `IdCliente` (= `IdUsuario`), teléfono, dirección, tipo de cliente |
| `Categorias` | Nuestra | Categorías |
| `Proveedores` | Nuestra | Proveedores |
| `Productos` | Nuestra | Productos (con precio de venta y compra, stock, mínimo, imagen) |
| `Inventarios` | Nuestra | Inventario de cada producto |
| `FormasPago` | Nuestra | Formas de pago |
| `Ventas` / `DetalleVentas` | Nuestra | Ventas y sus líneas |
| `Compras` / `DetalleCompras` | Nuestra | Compras a proveedores y sus líneas |
| `__EFMigrationsHistory` | EF Core | Migraciones aplicadas |

Se pueden ver desde Supabase en **Table Editor**.

## 9.4 Cuando se cambia un modelo

Si más adelante se agrega o modifica un campo (por ejemplo `public string? Codigo` en `Producto`),
se genera una nueva migración y la app la aplica sola al arrancar:

```bash
dotnet ef migrations add AgregarCodigoProducto
```

> Si la base de Supabase ya tenía tablas de una versión anterior del proyecto (por ejemplo `Products`
> u `Orders`), hay que borrarlas (o crear un proyecto nuevo en Supabase) antes de arrancar, para que
> la migración cree el esquema nuevo sin conflictos.

---

# Parte 10 — Ejecutar y probar el sistema

## 10.1 Ejecutar en la computadora (desarrollo)

1. Abrir una terminal en la carpeta del proyecto:

```bash
cd FerreteriaApp
```

2. Poner el connection string de Supabase en `appsettings.json` o en `appsettings.Development.json`
   (ver 2.5).

3. Restaurar paquetes y ejecutar:

```bash
dotnet restore
dotnet run
```

4. La consola muestra `Now listening on: http://localhost:5047`. Abrir esa URL. La primera vez tarda
   un poco más porque crea las tablas y carga los datos iniciales.

En Visual Studio: abrir `FerreteriaApp.csproj`, elegir el perfil **http** y apretar F5.

## 10.2 Usuarios iniciales

| Rol | Email | Contraseña |
|---|---|---|
| Admin (empleado, cargo Administrador) | `juan@gmail.com` | `Juan1234` |
| Empleado (vendedor de ejemplo) | `vendedor@ferreteria.com` | `Vendedor123` |

El administrador se define en `appsettings.json` → `"Admin"` (cambiarlo antes de entregar). Las
contraseñas deben cumplir la regla del diagrama: 8 caracteres con mayúscula, minúscula y número.
Los usuarios que se registran desde la web son **Clientes**; los empleados los crea el administrador
en **Gestión → Usuarios y empleados**.

## 10.3 Recorrido de prueba completo

**Como visitante**

1. Portada: categorías con la cantidad de productos y destacados.
2. **Catálogo**: buscar "taladro", filtrar por "Herramientas Eléctricas", cambiar de página.
3. Entrar a un producto y **agregarlo al carrito** (sin cuenta): el numerito del carrito sube.
4. En el carrito, tocar **Finalizar compra**: pide iniciar sesión.

**Como cliente**

5. **Registrarse** (nombre, apellido, email, contraseña de 8+ con mayúscula/minúscula/número,
   teléfono de 8-15 dígitos, dirección). Probar una contraseña débil o un teléfono con letras: se
   muestran los mensajes de validación.
6. Al quedar logueado, el carrito sigue ahí. **Finalizar compra**: elegir forma de pago y confirmar.
7. Aparece la **compra #N** en estado *Pendiente*; en **Mis compras** está en la lista. En el
   catálogo el stock bajó.
8. **Cancelar compra**: pasa a *Cancelada* y el stock vuelve.
9. Comprar de nuevo (para que quede una venta pendiente) y editar el **perfil** / **cambiar contraseña**.

**Como empleado** (`vendedor@ferreteria.com` / `Vendedor123`)

10. Al ingresar aparece el **Panel general** y el menú **Gestión**.
11. **Ventas**: abrir la venta web pendiente y pasarla a *Completada* (queda registrado el empleado);
    probar *Devuelta* (repone stock).
12. **Registrar venta** de mostrador: elegir el cliente, forma de pago, agregar filas de productos y
    confirmar. Probar una cantidad mayor al stock: error "stock insuficiente".
13. **Compras a proveedores → Registrar compra**: proveedor, productos, cantidades y precio
    unitario. Queda *Pendiente*. Abrirla y pasarla a *Completada*: el stock sube y el precio de
    compra del producto se actualiza. Intentar cancelarla después de vender esas unidades: error
    "no puede quedar negativo".
14. **Inventario**: filtrar "Bajo mínimo", **ajustar** el stock de un producto tras un recuento.
15. **Productos**: crear uno nuevo (con categoría, proveedor, precios, stock, imagen) y editarlo.
16. Intentar entrar a **Categorías** o **Usuarios**: *Acceso denegado* (son solo de Admin).

**Como administrador** (`juan@gmail.com` / `Juan1234`)

17. **Categorías**, **Proveedores** y **Formas de pago**: crear, editar y "eliminar" una que esté en
    uso (se desactiva; una categoría desactivada oculta sus productos del catálogo).
18. **Usuarios y empleados**: crear un empleado, hacerlo Admin y volver a Empleado, desactivar al
    cliente registrado (ya no puede iniciar sesión) y reactivarlo, cambiarlo a Mayorista.
19. **Eliminar producto**: uno sin movimientos se borra (con su inventario); uno vendido se desactiva.

---

# Parte 11 — Publicar en internet: GitHub + Render + Supabase

La base de datos ya está en Supabase. Falta que la aplicación corra en un servidor: para eso se sube
el código a GitHub y Render lo construye y lo publica.

## 11.1 `Dockerfile` — la receta para armar la aplicación

{{FILE:Dockerfile:dockerfile}}

Tiene dos etapas: **build** (imagen con el SDK, restaura paquetes y hace `dotnet publish`) y
**final** (imagen con solo el runtime, más liviana, que ejecuta `dotnet FerreteriaApp.dll`).

## 11.2 `.dockerignore`

{{FILE:.dockerignore:text}}

## 11.3 `render.yaml` — Blueprint de Render

{{FILE:render.yaml:yaml}}

`sync: false` significa "esta variable la cargo yo a mano en el panel" (no queda en el repositorio).

## 11.4 `.gitignore`

{{FILE:.gitignore:text}}

## 11.5 Subir el proyecto a GitHub

1. Crear un repositorio vacío en GitHub (botón **New**). No agregar README ni .gitignore.
2. Desde la carpeta `FerreteriaApp`, verificar que `appsettings.json` **no** tenga la contraseña real
   (o que esté en `appsettings.Development.json`, que se ignora). Luego:

```bash
git init
git add .
git commit -m "Sistema de ferretería con ASP.NET Core MVC y Supabase"
git branch -M main
git remote add origin https://github.com/<tu-usuario>/<tu-repo>.git
git push -u origin main
```

## 11.6 Crear el servicio en Render

**Opción A — Blueprint (usa `render.yaml`):** **New → Blueprint**, conectar el repositorio y cargar
las variables `ConnectionStrings__DefaultConnection` (connection string de Supabase) y, opcional,
`Admin__Password`.

**Opción B — Manual:** **New → Web Service**, conectar el repositorio, runtime **Docker**, plan Free,
y en **Environment Variables**:

| Key | Value |
|---|---|
| `ConnectionStrings__DefaultConnection` | `Host=aws-0-...pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.xxx;Password=...;SSL Mode=Require;Trust Server Certificate=true` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Admin__Password` | (opcional; debe cumplir la regla de 8 caracteres con mayúscula, minúscula y número) |

**Create Web Service**: Render construye la imagen y publica la app en
`https://ferreteria-app.onrender.com` (o el nombre elegido).

> En .NET, el doble guion bajo `__` en una variable de entorno equivale a los dos puntos `:` de
> `appsettings.json`: `ConnectionStrings__DefaultConnection` = `ConnectionStrings:DefaultConnection`.

Cada `git push` a `main` vuelve a desplegar automáticamente. Los logs del arranque (migraciones,
errores) están en la pestaña **Logs** de Render.

## 11.7 Notas sobre Render (plan Free)

- El servicio se "duerme" tras 15 minutos sin visitas; la primera visita después tarda ~30-60 s.
- El disco es **efímero**: las imágenes que suba el personal desde la app se pierden en cada nuevo
  deploy. Las 43 imágenes iniciales sí persisten porque están en el repositorio. Para imágenes nuevas
  conviene pegar la **URL de una imagen** alojada afuera (por ejemplo en Supabase Storage).
- La **sesión** (carrito) está en memoria: si Render reinicia el servicio, los carritos en curso se
  vacían. Las ventas ya confirmadas están en la base y no se pierden.

## 11.8 Cómo se conectan las tres piezas

```
   Navegador del usuario
           │  https
           ▼
   RENDER  (corre nuestra app .NET dentro de Docker)
           │  connection string (variable de entorno)
           ▼
   SUPABASE (PostgreSQL con las tablas creadas por EF Core)
```

---

# Parte 12 — Personalización

En `appsettings.json`:

```json
"Tienda": {
  "Nombre": "Ferretería Central",
  "Moneda": "Bs",
  "ZonaHoraria": "America/La_Paz"
}
```

- `Nombre`: barra, portada y pie de página. `Moneda`: símbolo de los precios (`Bs`, `S/`, `$`).
  `ZonaHoraria`: para mostrar fechas (`America/La_Paz`, `America/Lima`,
  `America/Argentina/Buenos_Aires`). En Render también se pueden cambiar con `Tienda__Nombre`, etc.

Otros lugares para personalizar:

- Dirección, teléfono y horario del pie: `Views/Shared/_Layout.cshtml`.
- Texto de la portada: `Views/Home/Index.cshtml`.
- Colores: `wwwroot/css/site.css`.
- Productos por página: `ProductosPorPagina` en `ProductosController.cs`.
- Reglas de contraseña: `Program.cs` → `AddIdentity`.
- Vendedor de ejemplo, proveedores y productos iniciales: `Data/DbInitializer.cs`.

---

# Parte 13 — Solución de problemas frecuentes

| Error / síntoma | Causa y solución |
|---|---|
| *Timeout* / no conecta a la base de datos | Se está usando el host de conexión directa (`db.xxx.supabase.co`), que es IPv6. Usar el connection string del **Session pooler** (2.2). |
| `password authentication failed` | Contraseña incorrecta en el connection string. Resetearla en Supabase → *Database → Reset database password*. |
| `EntityFrameworkCore does not exist`, `IdentityDbContext<> could not be found` | Los paquetes NuGet no se restauraron. Reemplazar el `.csproj` por el de 2.4.1, borrar `bin/` y `obj/`, correr `dotnet restore` y `dotnet build`. |
| `dotnet ef` no se reconoce como comando | `dotnet tool install --global dotnet-ef` y reabrir la terminal. |
| `Unable to create an object of type ApplicationDbContext` | Falta `Microsoft.EntityFrameworkCore.Design` o la terminal no está en la carpeta del `.csproj`. |
| `relation "Products" already exists` u otras tablas viejas | La base tenía otra versión del modelo. Borrar las tablas viejas (o crear un proyecto nuevo en Supabase) y volver a arrancar. |
| `The view '...' was not found` | Falta el `.cshtml` de esa acción; revisar el nombre exacto en `Views/<Controlador>/`. |
| 400/403 al enviar un formulario | Falta el token antifalsificación: el `<form>` debe tener `method="post"` y usar los tag helpers. |
| Al registrarse dice que la contraseña no cumple | Regla del diagrama: mínimo 8 caracteres con mayúscula, minúscula y número (`Program.cs` → `AddIdentity`). |
| "El teléfono debe tener solo números (8 a 15 dígitos)" | Regla del diagrama (`[RegularExpression]` en los modelos). Sin espacios ni guiones. |
| El login siempre falla | Identity compara el `UserName` (= email). Revisar mayúsculas y que la cuenta no esté desactivada ("La cuenta está desactivada"). |
| Al crear un producto el precio `99.50` se guarda como `9950` | Falta la cultura invariante en `Program.cs`. |
| "stock insuficiente" o "no puede quedar negativo" | Reglas del diagrama: no se vende sin stock y no se cancela una compra si dejaría stock negativo. Revisar el **Inventario**. |
| La imagen subida no se ve | El `<form>` necesita `enctype="multipart/form-data"` y `Program.cs` debe tener `app.UseStaticFiles()`. |
| El carrito se vació solo | La sesión expira a las 2 horas de inactividad o cuando se reinicia el servidor. |
| En Render arranca pero da error 500 | Falta `ConnectionStrings__DefaultConnection` o es incorrecta. Ver *Logs*. |
| Los precios se ven con otro símbolo | Cambiar `"Tienda:Moneda"` (o `Tienda__Moneda` en Render). |

---

# Parte 14 — Preguntas típicas para defender el proyecto

**¿Cómo se implementó la herencia Usuario → Empleado / Cliente del diagrama?** Con herencia de
clases en C# (`class Empleado : Usuario`) y estrategia **TPT** en EF Core: una tabla `Usuarios` con
los datos comunes y `Empleados` / `Clientes` con los propios, cuya clave primaria es también clave
foránea a `Usuarios`. Un usuario es exactamente una de las dos cosas.

**¿Por qué Identity si el diagrama tiene su propia clase Usuario?** Porque programar el login y el
guardado de contraseñas a mano es inseguro. `Usuario` hereda de `IdentityUser<int>` (así el
`idUsuario` es un `int` como en el diagrama) y las tablas se renombran para respetar los nombres.
Identity aporta hash de contraseñas, cookies y roles ya probados.

**¿Dónde está el rol del usuario?** En `UsuarioRoles` (tabla estándar de Identity que relaciona
`Usuarios` con `Roles`). La aplicación asigna exactamente un rol: `Admin`, `Empleado` o `Cliente`.

**¿Por qué el carrito no es una tabla?** Porque no está en el diagrama. Vive en la sesión del
servidor (JSON) y cualquier visitante puede armarlo; al confirmar se convierte en `Venta` +
`DetalleVenta`, que sí son tablas.

**¿Para qué sirve Inventario si Producto ya tiene stock?** `Producto.Stock` es el dato operativo
(catálogo, ventas); `Inventario` es el registro de control con stock actual, mínimo y **última
actualización**, que permite auditar cuándo cambió y listar lo que está bajo mínimo. Se mantienen
sincronizados en `Producto.ActualizarStock()`.

**¿Cómo se cumple "no permitir vender si no hay stock"?** `Venta.AgregarDetalle` verifica
`producto.Stock >= cantidad` y `Producto.ActualizarStock` lanza una excepción si quedaría negativo.
Además el checkout vuelve a leer el stock de la base justo antes de confirmar, dentro de una
transacción.

**¿Y "no permitir compra con stock negativo"?** Al cancelar una compra ya completada se descuenta
lo que había ingresado; si eso dejara stock negativo, la excepción hace que la transacción se deshaga
y se informa el error.

**¿Por qué el checkout usa una transacción?** Porque crea la venta, sus detalles y descuenta el
stock de varios productos. O se hace todo o no se hace nada.

**¿Por qué `DetalleVenta` guarda `PrecioUnitario` y `Subtotal`?** Para que el historial refleje lo
que se cobró en ese momento aunque el precio del producto cambie después (como una factura).

**¿Qué diferencia hay entre una venta web y una de mostrador?** La web la crea el cliente desde el
carrito y queda *Pendiente* sin empleado; un empleado la completa (`cerrarVenta` registra quién).
La de mostrador la registra un empleado eligiendo cliente y productos, y nace *Completada*.

**¿Qué pasa con el stock en cada estado de venta?** *Pendiente* y *Completada* lo tienen descontado;
*Cancelada* y *Devuelta* lo reponen. Al cambiar de un grupo a otro, `CambiarEstado` ajusta el stock.

**¿Cómo se guardan las contraseñas?** Identity guarda un *hash* con sal en `Usuarios.PasswordHash`;
la contraseña en texto plano nunca se almacena.

**¿Cómo se validan los datos?** Con Data Annotations en los modelos (`[Required]`, `[Range]`,
`[RegularExpression]`...), que ASP.NET aplica en el servidor (`ModelState.IsValid`) y en el navegador
(jQuery Validation), más las reglas de contraseña de Identity y las verificaciones de stock en el
código.

**¿Qué hace `[ValidateAntiForgeryToken]`?** Cada formulario lleva un token oculto único. Si otro
sitio intentara enviar un formulario en nombre del usuario (CSRF), no tendría el token y se rechaza.

**¿Qué es una migración?** Un archivo generado por EF Core que crea o modifica las tablas a partir de
los modelos. Reemplaza los scripts SQL manuales y permite versionar la base junto con el código.

**¿Por qué el Session pooler de Supabase?** La conexión directa es solo IPv6 en muchas redes y da
timeout; el pooler tiene IPv4 y administra las conexiones.

**¿Por qué `DateTime.UtcNow`?** El servidor de Render está en otra zona horaria; se guarda en UTC y
se convierte a la hora local solo al mostrar (`Formato.Fecha`).

---

# Anexo A — README.md del proyecto

A continuación se reproduce íntegro el archivo `README.md` que acompaña al código:

{{README}}
