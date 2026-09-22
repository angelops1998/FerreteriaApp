// Gráficos de la sección de reportes (usa Chart.js, que se carga desde wwwroot/lib).
// Cada vista llama a reportes.barras(...), reportes.lineas(...) o reportes.torta(...)
// pasándole los datos que preparó el controlador.
(function () {
    "use strict";

    // Colores de la ferretería: amarillo/naranja de la marca + apoyos
    var PALETA = ["#ffc107", "#0d6efd", "#198754", "#dc3545", "#6f42c1", "#fd7e14", "#20c997", "#6c757d", "#d63384", "#0dcaf0"];

    var moneda = document.body.dataset.moneda || "Bs";

    function precio(valor) {
        return moneda + " " + Number(valor).toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    // Opciones comunes: sin animación (así el gráfico también sale bien al imprimir),
    // altura controlada por el contenedor y tooltip con el importe formateado.
    function opcionesBase(esMoneda) {
        return {
            responsive: true,
            maintainAspectRatio: false,
            animation: false,
            devicePixelRatio: 2, // más nitidez al imprimir o guardar como PDF
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: function (ctx) {
                            var v = ctx.parsed.y !== undefined && ctx.parsed.y !== null ? ctx.parsed.y : ctx.parsed.x;
                            if (v === undefined || v === null) v = ctx.parsed;
                            return (ctx.dataset.label ? ctx.dataset.label + ": " : "") + (esMoneda ? precio(v) : v);
                        }
                    }
                }
            }
        };
    }

    function ejeImporte(esMoneda) {
        return {
            beginAtZero: true,
            ticks: {
                callback: function (v) { return esMoneda ? precio(v) : v; }
            }
        };
    }

    // Si no hay datos, en vez de un gráfico vacío se muestra un mensaje
    function sinDatos(canvas) {
        var aviso = document.createElement("p");
        aviso.className = "text-muted text-center my-4";
        aviso.textContent = "Sin datos para graficar en este período.";
        canvas.parentNode.replaceChild(aviso, canvas);
    }

    function crear(id, config, hayDatos) {
        var canvas = document.getElementById(id);
        if (!canvas) return null;
        if (!hayDatos) { sinDatos(canvas); return null; }
        return new Chart(canvas, config);
    }

    window.reportes = {
        // Barras verticales u horizontales. series = [{ label, datos, color }]
        barras: function (id, config) {
            var esMoneda = config.moneda !== false;
            var horizontal = config.horizontal === true;
            var opciones = opcionesBase(esMoneda);
            opciones.indexAxis = horizontal ? "y" : "x";
            opciones.plugins.legend.display = config.series.length > 1;
            opciones.scales = horizontal ? { x: ejeImporte(esMoneda) } : { y: ejeImporte(esMoneda) };

            return crear(id, {
                type: "bar",
                data: {
                    labels: config.labels,
                    datasets: config.series.map(function (s, i) {
                        return {
                            label: s.label || "",
                            data: s.datos,
                            // s.colores permite un color distinto por barra (ej. según el estado)
                            backgroundColor: s.colores || s.color || PALETA[i % PALETA.length],
                            borderRadius: 4,
                            maxBarThickness: 44
                        };
                    })
                },
                options: opciones
            }, config.labels.length > 0);
        },

        // Línea de evolución (ventas o compras por día)
        lineas: function (id, config) {
            var esMoneda = config.moneda !== false;
            var opciones = opcionesBase(esMoneda);
            opciones.plugins.legend.display = config.series.length > 1;
            opciones.scales = { y: ejeImporte(esMoneda) };

            return crear(id, {
                type: "line",
                data: {
                    labels: config.labels,
                    datasets: config.series.map(function (s, i) {
                        var color = s.color || PALETA[i % PALETA.length];
                        return {
                            label: s.label || "",
                            data: s.datos,
                            borderColor: color,
                            backgroundColor: color + "33",
                            borderWidth: 2,
                            pointRadius: 3,
                            tension: 0.3,
                            fill: config.series.length === 1
                        };
                    })
                },
                options: opciones
            }, config.labels.length > 0);
        },

        // Torta con agujero (formas de pago, roles, situación del stock...)
        torta: function (id, config) {
            var esMoneda = config.moneda !== false;
            var opciones = opcionesBase(esMoneda);
            opciones.plugins.legend = { display: true, position: "right", labels: { boxWidth: 12, padding: 10 } };
            opciones.plugins.tooltip.callbacks.label = function (ctx) {
                var total = ctx.dataset.data.reduce(function (a, b) { return a + b; }, 0);
                var porcentaje = total === 0 ? 0 : Math.round(ctx.parsed / total * 100);
                return ctx.label + ": " + (esMoneda ? precio(ctx.parsed) : ctx.parsed) + " (" + porcentaje + "%)";
            };

            return crear(id, {
                type: "doughnut",
                data: {
                    labels: config.labels,
                    datasets: [{
                        data: config.datos,
                        backgroundColor: config.colores || config.labels.map(function (_, i) { return PALETA[i % PALETA.length]; }),
                        borderWidth: 2,
                        borderColor: "#fff"
                    }]
                },
                options: opciones
            }, config.datos.some(function (v) { return v > 0; }));
        },

        // Abre el diálogo de impresión del navegador (ahí se elige "Guardar como PDF").
        // El nombre del archivo que propone el navegador es el título de la página,
        // así que se cambia un momento y después se deja como estaba.
        imprimir: function (nombreArchivo) {
            var titulo = document.title;
            if (nombreArchivo) document.title = nombreArchivo;
            window.print();
            setTimeout(function () { document.title = titulo; }, 500);
        }
    };
})();
