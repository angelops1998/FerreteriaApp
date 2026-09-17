using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FerreteriaApp.Models;

namespace FerreteriaApp.Data
{
    // Carga los datos iniciales la primera vez que arranca la app:
    // roles, administrador, un empleado de ejemplo, formas de pago, categorías, proveedores,
    // productos e inventario. Si ya hay datos, no hace nada (se puede reiniciar sin problema).
    public static class DbInitializer
    {
        public const string RolAdmin = "Admin";
        public const string RolEmpleado = "Empleado";
        public const string RolCliente = "Cliente";

        public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<Usuario>>();
            var roleManager = services.GetRequiredService<RoleManager<Rol>>();

            // 1. Roles
            var roles = new[]
            {
                new Rol(RolAdmin, "Administrador del sistema: acceso total"),
                new Rol(RolEmpleado, "Personal de la ferretería: ventas, compras e inventario"),
                new Rol(RolCliente, "Cliente registrado: compra desde el catálogo"),
            };
            foreach (var rol in roles)
            {
                if (!await roleManager.RoleExistsAsync(rol.Name!))
                    await roleManager.CreateAsync(rol);
            }

            // 2. Administrador (es un Empleado con rol Admin). Email y contraseña vienen de appsettings.json
            var adminEmail = config["Admin:Email"] ?? "admin@ferreteria.com";
            var adminPassword = config["Admin:Password"] ?? "Admin1234";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new Empleado
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    Nombre = config["Admin:Nombre"] ?? "Administrador",
                    Apellido = config["Admin:Apellido"] ?? "Ferretería",
                    Cargo = "Administrador",
                    Telefono = "70000000",
                    Direccion = "Oficina central",
                    Estado = true
                };
                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, RolAdmin);
            }

            // 3. Un empleado de ejemplo (vendedor) para probar el rol Empleado
            const string vendedorEmail = "vendedor@ferreteria.com";
            if (await userManager.FindByEmailAsync(vendedorEmail) == null)
            {
                var vendedor = new Empleado
                {
                    UserName = vendedorEmail,
                    Email = vendedorEmail,
                    EmailConfirmed = true,
                    Nombre = "Carlos",
                    Apellido = "Mamani",
                    Cargo = "Vendedor",
                    Telefono = "71234567",
                    Direccion = "Av. Principal 123",
                    Estado = true
                };
                if ((await userManager.CreateAsync(vendedor, "Vendedor123")).Succeeded)
                    await userManager.AddToRoleAsync(vendedor, RolEmpleado);
            }

            // 4. Formas de pago
            if (!await context.FormasPago.AnyAsync())
            {
                context.FormasPago.AddRange(
                    new FormaPago { Nombre = "Efectivo", Descripcion = "Pago en efectivo al recibir o en mostrador" },
                    new FormaPago { Nombre = "Tarjeta", Descripcion = "Tarjeta de débito o crédito" },
                    new FormaPago { Nombre = "Transferencia", Descripcion = "Transferencia bancaria" },
                    new FormaPago { Nombre = "QR", Descripcion = "Pago con código QR" });
                await context.SaveChangesAsync();
            }

            // 5. Categorías, proveedores, productos e inventario (solo si no hay productos)
            if (await context.Productos.AnyAsync()) return;

            var categorias = new Dictionary<string, Categoria>
            {
                ["Herramientas Eléctricas"] = new() { Nombre = "Herramientas Eléctricas", Descripcion = "Taladros, amoladoras, sierras y más herramientas con motor." },
                ["Herramientas Manuales"] = new() { Nombre = "Herramientas Manuales", Descripcion = "Llaves, alicates, martillos, sierras y herramientas de mano." },
                ["Electricidad"] = new() { Nombre = "Electricidad", Descripcion = "Instrumentos de medición, cables, linternas y accesorios eléctricos." },
                ["Plomería y Baño"] = new() { Nombre = "Plomería y Baño", Descripcion = "Grifería, sanitarios y accesorios de plomería." },
                ["Construcción"] = new() { Nombre = "Construcción", Descripcion = "Materiales y herramientas para obra." },
                ["Pintura"] = new() { Nombre = "Pintura", Descripcion = "Brochas, rodillos y espátulas para pintar." },
                ["Jardinería"] = new() { Nombre = "Jardinería", Descripcion = "Herramientas para el cuidado de jardines y áreas verdes." },
                ["Accesorios y Cerrajería"] = new() { Nombre = "Accesorios y Cerrajería", Descripcion = "Discos de corte, bisagras, manijas y repuestos." },
            };
            context.Categorias.AddRange(categorias.Values);

            var proveedores = new Dictionary<string, Proveedor>
            {
                ["Bosch"] = new() { Nombre = "Robert Bosch Bolivia S.R.L.", Ruc = "1020304050", Telefono = "22445566", Direccion = "Av. Blanco Galindo Km 4, Cochabamba" },
                ["DeWalt"] = new() { Nombre = "DeWalt Andina S.A.", Ruc = "1030405060", Telefono = "33556677", Direccion = "Parque Industrial PI-12, Santa Cruz" },
                ["Stanley"] = new() { Nombre = "Stanley Bolivia S.A.", Ruc = "1040506070", Telefono = "22667788", Direccion = "Calle Comercio 456, La Paz" },
                ["Genérico"] = new() { Nombre = "Importadora Ferretera Central S.R.L.", Ruc = "1050607080", Telefono = "44778899", Direccion = "Av. 6 de Agosto 789, Cochabamba" },
                ["Profesional"] = new() { Nombre = "Herramientas Profesionales Import S.R.L.", Ruc = "1060708090", Telefono = "33889900", Direccion = "Av. Cristo Redentor 321, Santa Cruz" },
                ["Daewoo"] = new() { Nombre = "Maquinarias Daewoo Bolivia S.A.", Ruc = "1070809010", Telefono = "22990011", Direccion = "Zona Industrial Villa Fátima, La Paz" },
            };
            context.Proveedores.AddRange(proveedores.Values);
            await context.SaveChangesAsync();

            // Función local para armar cada producto con menos repetición.
            // El precio de compra se estima como el 70% del precio de venta.
            Producto P(string nombre, decimal precioVenta, int stock, int stockMinimo, string categoria, string proveedor, string imagen, string descripcion) => new()
            {
                Nombre = nombre,
                Descripcion = descripcion,
                PrecioVenta = precioVenta,
                PrecioCompra = Math.Round(precioVenta * 0.7m, 2),
                Stock = stock,
                StockMinimo = stockMinimo,
                Estado = true,
                Categoria = categorias[categoria],
                Proveedor = proveedores[proveedor],
                ImagenUrl = "/images/productos/" + imagen + ".png"
            };

            var productos = new List<Producto>
            {
                // Herramientas Eléctricas
                P("Amoladora Bosch", 350, 15, 3, "Herramientas Eléctricas", "Bosch", "amoladora-bosch",
                  "Amoladora angular Bosch de 4 1/2\" (115 mm) con motor de 720 W. Ideal para cortar, desbastar y pulir metal, piedra y cerámica. Empuñadura auxiliar y protección contra rearranque."),
                P("Amoladora DeWalt", 380, 12, 3, "Herramientas Eléctricas", "DeWalt", "amoladora-dewalt",
                  "Amoladora angular DeWalt de 4 1/2\" con motor de 850 W y 11.000 rpm. Cuerpo compacto, interruptor deslizante con bloqueo y protector ajustable sin herramientas."),
                P("Taladro Bosch", 340, 18, 3, "Herramientas Eléctricas", "Bosch", "taladro-bosch",
                  "Taladro percutor Bosch GSB 16 RE de 750 W con mandril de 13 mm. Velocidad variable, reversible, con función percutora para perforar concreto y mampostería."),
                P("Taladro Atornillador Bosch", 350, 10, 3, "Herramientas Eléctricas", "Bosch", "taladro-atornillador-bosch",
                  "Taladro atornillador inalámbrico Bosch de 18 V con batería de litio, 2 velocidades y 20 posiciones de torque. Incluye cargador y maletín."),
                P("Sierra Circular Bosch", 450, 8, 2, "Herramientas Eléctricas", "Bosch", "sierra-circular-bosch",
                  "Sierra circular Bosch GKS 190 de 1400 W con disco de 7 1/4\" (184 mm). Corte a 90° hasta 70 mm de profundidad, con guía paralela y ajuste de bisel hasta 45°."),
                P("Motosierra Daewoo", 900, 5, 2, "Herramientas Eléctricas", "Daewoo", "motosierra-daewoo",
                  "Motosierra a gasolina Daewoo con motor de 2 tiempos de 52 cc y espada de 20\". Arranque fácil, freno de cadena y lubricación automática. Ideal para poda y tala."),

                // Herramientas Manuales
                P("Martillo Genérico", 50, 40, 10, "Herramientas Manuales", "Genérico", "martillo-generico",
                  "Martillo de carpintero con cabeza de acero forjado de 16 oz y uña para extraer clavos. Mango ergonómico con recubrimiento antideslizante."),
                P("Llave Cruz Genérico", 70, 20, 5, "Herramientas Manuales", "Genérico", "llave-cruz-generico",
                  "Llave cruz de acero cromado para ruedas de vehículo con medidas 17, 19, 21 y 23 mm. Brazos de 14\" para mayor palanca."),
                P("Llave Inglesa Ajustable Genérico", 90, 25, 5, "Herramientas Manuales", "Genérico", "llave-inglesa-ajustable-generico",
                  "Llave inglesa ajustable de 10\" (250 mm) en acero al cromo vanadio con acabado cromado. Apertura máxima de 30 mm, escala grabada en la mordaza."),
                P("Alicate Stanley", 30, 35, 10, "Herramientas Manuales", "Stanley", "alicate-stanley",
                  "Alicate universal Stanley de 8\" con mordazas de acero forjado y mangos bimaterial. Corta, sujeta y dobla alambre con precisión."),
                P("Alicate de Presión Bosi", 55, 22, 5, "Herramientas Manuales", "Profesional", "alicate-de-presion-bosi",
                  "Alicate de presión Bosi de 10\" con mordaza curva. Tornillo de ajuste y palanca de liberación rápida. Acero al cromo molibdeno."),
                P("Metro Genérico", 35, 50, 10, "Herramientas Manuales", "Genérico", "metro-generico",
                  "Flexómetro de 5 metros con cinta de acero de 19 mm, freno automático, gancho magnético y clip para cinturón. Carcasa con recubrimiento de goma."),
                P("Sierra de Madera Stanley", 70, 18, 5, "Herramientas Manuales", "Stanley", "sierra-de-madera-stanley",
                  "Serrucho Stanley FatMax de 20\" (500 mm) con dientes templados de triple filo para cortes rápidos en madera. Mango ergonómico bimaterial."),
                P("Sierra Mecánica Genérico", 200, 10, 3, "Herramientas Manuales", "Genérico", "sierra-mecanica-generico",
                  "Arco de sierra metálico de 12\" con marco tubular reforzado y tensor de hoja. Incluye hoja bimetálica de 24 dientes por pulgada para cortar metal y PVC."),
                P("Llave de Carraca Genérico", 50, 20, 5, "Herramientas Manuales", "Genérico", "llave-de-carraca-generico",
                  "Llave de carraca (ratchet) de encastre 1/2\" con mecanismo de 72 dientes y palanca de reversa. Mango antideslizante, acero al cromo vanadio."),
                P("Llave Allen Proto", 55, 15, 5, "Herramientas Manuales", "Profesional", "llave-allen-proto",
                  "Juego de llaves Allen Proto de 13 piezas en medidas milimétricas (1.5 a 10 mm) con extremo de bola. Incluye organizador plegable."),
                P("Juego de Llaves Klein Tools", 400, 6, 2, "Herramientas Manuales", "Profesional", "juego-de-llaves-klein-tools",
                  "Juego de 14 llaves combinadas Klein Tools en medidas milimétricas (6 a 24 mm). Acero forjado con acabado cromado pulido e incluye estuche enrollable."),
                P("Cúter Genérico", 12, 60, 15, "Herramientas Manuales", "Genérico", "cuter-generico",
                  "Cúter profesional de 18 mm con cuerpo metálico, bloqueo automático de hoja y hoja segmentada de acero SK5. Se vende por unidad."),
                P("Nivel de Burbuja Johnson", 35, 25, 5, "Herramientas Manuales", "Profesional", "nivel-de-burbuja-johnson",
                  "Nivel de aluminio Johnson de 24\" (60 cm) con 3 burbujas (horizontal, vertical y 45°). Base fresada y tapas de goma antigolpes."),
                P("Calibrador KTC", 70, 12, 3, "Herramientas Manuales", "Profesional", "calibrador-ktc",
                  "Calibrador vernier KTC de 150 mm en acero inoxidable con escala en mm y pulgadas. Precisión de 0.02 mm, mide exterior, interior y profundidad."),
                P("Caja de Herramientas Genérico", 100, 14, 3, "Herramientas Manuales", "Genérico", "caja-de-herramientas-generico",
                  "Caja de herramientas plástica de 20\" con bandeja extraíble, organizadores en la tapa y cierres metálicos. Resistente a golpes y con asa reforzada."),
                P("Hacha Genérico", 120, 10, 3, "Herramientas Manuales", "Genérico", "hacha-generico",
                  "Hacha de leñador con cabeza de acero forjado de 1.5 kg y mango de madera de 70 cm. Filo templado, ideal para cortar leña y trabajos de campo."),

                // Electricidad
                P("Multímetro Facom", 420, 8, 2, "Electricidad", "Profesional", "multimetro-facom",
                  "Multímetro digital Facom con pantalla retroiluminada. Mide voltaje AC/DC, corriente, resistencia, continuidad y capacitancia. Incluye puntas de prueba y funda protectora."),
                P("Cable Extensor Genérico", 40, 30, 10, "Electricidad", "Genérico", "cable-extensor-generico",
                  "Extensión eléctrica de 10 metros con cable 2x1.5 mm, enchufe y toma polarizados. Soporta hasta 10 A, ideal para uso en interiores y talleres."),
                P("Linterna Genérico", 25, 45, 10, "Electricidad", "Genérico", "linterna-generico",
                  "Linterna LED de aluminio con alcance de 200 metros y 3 modos de luz. Resistente al agua, funciona con 2 pilas D (no incluidas)."),
                P("Pela Cables Genérico", 80, 15, 5, "Electricidad", "Genérico", "pela-cables-generico",
                  "Pelacables automático para conductores de 0.5 a 6 mm² con cortador integrado y crimpadora de terminales. Mangos ergonómicos aislados."),

                // Plomería y Baño
                P("Grifo Genérico", 120, 12, 3, "Plomería y Baño", "Genérico", "grifo-generico",
                  "Grifo monocomando para lavamanos con acabado cromado y cartucho cerámico de 35 mm. Incluye flexibles de conexión de 1/2\"."),
                P("Inodoro Genérico", 620, 4, 2, "Plomería y Baño", "Genérico", "inodoro-generico",
                  "Inodoro de dos piezas en porcelana blanca con doble descarga (3/6 litros) y asiento de cierre lento. Salida horizontal, incluye kit de instalación."),

                // Construcción
                P("Pala Genérico", 90, 20, 5, "Construcción", "Genérico", "pala-generico",
                  "Pala punta de corazón con hoja de acero templado y mango de madera de 120 cm con empuñadura en D. Para excavación y movimiento de tierra."),
                P("Carretilla Genérico", 235, 8, 2, "Construcción", "Genérico", "carretilla-generico",
                  "Carretilla de obra con bandeja de acero galvanizado de 65 litros, chasis reforzado y rueda neumática de 16\". Capacidad de carga de 120 kg."),
                P("Calamina Genérico", 130, 40, 10, "Construcción", "Genérico", "calamina-generico",
                  "Calamina ondulada de acero galvanizado y prepintado, calibre 28, de 0.80 x 3.00 metros. Colores disponibles: azul, rojo y natural. Precio por unidad."),
                P("Ladrillo", 5, 2000, 200, "Construcción", "Genérico", "ladrillo",
                  "Ladrillo de arcilla cocida de 6 huecos, medidas 24 x 12 x 6 cm. Alta resistencia para muros y tabiques. Precio por unidad, consulte por precio por millar."),
                P("Escalera de Aluminio Genérico", 220, 6, 2, "Construcción", "Genérico", "escalera-de-aluminio-generico",
                  "Escalera tipo tijera de aluminio con 6 peldaños antideslizantes y altura de 1.80 m. Soporta hasta 120 kg, liviana y plegable."),

                // Pintura
                P("Brocha Genérico", 15, 60, 15, "Pintura", "Genérico", "brocha-generico",
                  "Brocha de 3\" con cerdas naturales y mango de madera. Para pinturas al aceite, látex y barnices. Buena retención de pintura y acabado uniforme."),
                P("Rodillo Genérico", 30, 35, 10, "Pintura", "Genérico", "rodillo-generico",
                  "Rodillo de 9\" de lana sintética con soporte metálico y mango ergonómico. Ideal para pintar paredes y techos con látex."),
                P("Espátula Neken", 25, 30, 10, "Pintura", "Profesional", "espatula-neken",
                  "Espátula Neken de 3\" con hoja de acero inoxidable flexible y mango de madera. Para aplicar masilla, raspar y preparar superficies."),

                // Jardinería
                P("Podadora Genérico", 35, 15, 5, "Jardinería", "Genérico", "podadora-generico",
                  "Tijera podadora de setos con hojas de acero de 9\" y mangos de madera de 50 cm. Corte limpio para arbustos, cercos vivos y ramas delgadas."),
                P("Paleta Jardinera Genérico", 30, 25, 5, "Jardinería", "Genérico", "paleta-jardinera-generico",
                  "Pala de mano para jardín con hoja de acero inoxidable y mango de madera. Perfecta para trasplantar, remover tierra y sembrar en macetas."),
                P("Machete Genérico", 55, 20, 5, "Jardinería", "Genérico", "machete-generico",
                  "Machete de 18\" con hoja de acero al carbono y mango de polipropileno remachado. Para desmalezar, cortar ramas y trabajos de campo."),

                // Accesorios y Cerrajería
                P("Disco Diamante Bosch", 20, 50, 10, "Accesorios y Cerrajería", "Bosch", "disco-diamante-bosch",
                  "Disco de corte diamantado Bosch Standard de 4 1/2\" (115 mm) segmentado. Para cortar en seco concreto, ladrillo, piedra y cerámica."),
                P("Disco de Corte FirePower", 35, 45, 10, "Accesorios y Cerrajería", "Profesional", "disco-de-corte-firepower",
                  "Disco de corte y desbaste FirePower para metal, tipo 27. Disponible en 4 1/2\", 7\" y 9\". Grano abrasivo de óxido de aluminio de alta durabilidad."),
                P("Bisagra Genérico", 10, 100, 20, "Accesorios y Cerrajería", "Genérico", "bisagra-generico",
                  "Bisagra de acero inoxidable de 3\" x 3\" con pasador fijo y 4 orificios avellanados. Para puertas de madera y muebles. Precio por unidad."),
                P("Manija de Puerta Genérico", 230, 10, 3, "Accesorios y Cerrajería", "Genérico", "manija-de-puerta-generico",
                  "Manija con placa para puerta principal en acabado dorado, con bocallave para cerradura de embutir. Incluye tornillos de fijación."),
            };

            context.Productos.AddRange(productos);

            // Un registro de inventario por producto, con el mismo stock inicial
            context.Inventarios.AddRange(productos.Select(p => new Inventario
            {
                Producto = p,
                StockActual = p.Stock,
                StockMinimo = p.StockMinimo,
                UltimaActualizacion = DateTime.UtcNow
            }));

            await context.SaveChangesAsync();
        }
    }
}
