// ========== ACTIVAR LINK DEL MENÚ ACTUAL ==========
$(document).ready(function () {
    var path = window.location.pathname;
    $('.sidebar .nav-link').each(function () {
        if ($(this).attr('href') === path) {
            $(this).addClass('active');
        }
    });
});

// ========== HELPER: FORMATEAR FECHA ==========
function formatearFecha(fecha) {
    const opciones = {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    };
    return new Date(fecha).toLocaleDateString('es-GT', opciones);
}

// ========== HELPER: CALCULAR EDAD ==========
function calcularEdad(fechaNacimiento) {
    const hoy = new Date();
    const nacimiento = new Date(fechaNacimiento);
    let edad = hoy.getFullYear() - nacimiento.getFullYear();
    const mes = hoy.getMonth() - nacimiento.getMonth();

    if (mes < 0 || (mes === 0 && hoy.getDate() < nacimiento.getDate())) {
        edad--;
    }

    return edad;
}

// ========== CONFIRMAR ELIMINACIÓN ==========
function confirmarEliminacion(mensaje) {
    return confirm(mensaje || '¿Está seguro que desea eliminar este registro?');
}

// ========== INICIALIZAR SELECT2 ==========
function inicializarSelect2(selector, placeholder) {
    $(selector).select2({
        theme: 'bootstrap-5',
        placeholder: placeholder || 'Seleccione una opción',
        allowClear: true,
        width: '100%'
    });
}

// ========== VALIDAR FORMULARIO ==========
function validarCampoRequerido(valor, nombreCampo) {
    if (!valor || valor.trim() === '') {
        alert(`El campo ${nombreCampo} es requerido`);
        return false;
    }
    return true;
}

// ========== VALIDAR DPI (GUATEMALA) ==========
function validarDPI(dpi) {
    // DPI debe tener 13 dígitos
    const regex = /^\d{13}$/;
    return regex.test(dpi);
}

// ========== VALIDAR EMAIL ==========
function validarEmail(email) {
    const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return regex.test(email);
}

// ========== VALIDAR TELÉFONO (GUATEMALA) ==========
function validarTelefono(telefono) {
    // Formato: 8 dígitos, puede empezar con +502
    const regex = /^(\+502)?[2-7]\d{7}$/;
    return regex.test(telefono.replace(/\s/g, ''));
}

// ========== MOSTRAR LOADING ==========
function mostrarLoading(texto) {
    return `
        <div class="text-center py-5">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Cargando...</span>
            </div>
            <p class="mt-2 text-muted">${texto || 'Cargando...'}</p>
        </div>
    `;
}

// ========== BADGE DE ESTADO DE CITA ==========
function getBadgeEstadoCita(estado) {
    const badges = {
        'programada': '<span class="badge bg-warning">Programada</span>',
        'confirmada': '<span class="badge bg-info">Confirmada</span>',
        'atendida': '<span class="badge bg-success">Atendida</span>',
        'cancelada': '<span class="badge bg-danger">Cancelada</span>',
        'noAsistio': '<span class="badge bg-secondary">No Asistió</span>'
    };
    return badges[estado] || '<span class="badge bg-secondary">Desconocido</span>';
}

// ========== COLOR DE ESTADO DE CITA ==========
function getColorEstadoCita(estado) {
    const colores = {
        'programada': '#fbbf24',
        'confirmada': '#3b82f6',
        'atendida': '#10b981',
        'cancelada': '#ef4444',
        'noAsistio': '#6b7280'
    };
    return colores[estado] || '#6b7280';
}

// ========== AUTO-DISMISS ALERTS ==========
$(document).ready(function () {
    // Auto-cerrar alerts después de 5 segundos
    setTimeout(function () {
        $('.alert').fadeOut('slow', function () {
            $(this).remove();
        });
    }, 5000);
});

// ========== SCROLL TO TOP ==========
$(document).ready(function () {
    // Mostrar botón cuando se hace scroll
    $(window).scroll(function () {
        if ($(this).scrollTop() > 200) {
            $('#scrollTopBtn').fadeIn();
        } else {
            $('#scrollTopBtn').fadeOut();
        }
    });

    // Click para volver arriba
    $('#scrollTopBtn').click(function () {
        $('html, body').animate({ scrollTop: 0 }, 600);
        return false;
    });
});