var editorEntrada = CodeMirror.fromTextArea(document.getElementById('entrada'), {
    lineNumbers: true,
    mode: 'javascript',
    theme: 'dracula',
    lineWrapping: false 
});

var editorConsola = CodeMirror.fromTextArea(document.getElementById('consola'), {
    lineNumbers: true,
    mode: 'javascript',
    theme: 'dracula',
    readOnly: true, 
    lineWrapping: false
});

document.getElementById('btnEjecutar').onclick = async function () {
    const textoEntrada = editorEntrada.getValue();

    try {
        const response = await fetch('/Entrada', {
            method: 'POST',
            headers: {
                'Content-Type': 'text/plain'
            },
            body: textoEntrada
        });

        const result = await response.text();  
        console.log('Respuesta del servidor:', result);
        editorConsola.setValue(result);  
    } catch (error) {
        console.error('Error al enviar los datos:', error);
        editorConsola.setValue('Error al ejecutar el código');
    }
};


document.getElementById('btnArchivo').addEventListener('click', function () {
    const menu = document.getElementById('menuArchivo');
    menu.style.display = menu.style.display === 'block' ? 'none' : 'block';
});


document.getElementById('nuevoArchivo').addEventListener('click', function () {
    editorConsola.setValue('');
    editorEntrada.setValue('');
});


document.getElementById('guardarArchivo').addEventListener('click', function () {
    let fileName = prompt('Ingrese el nombre del archivo:', 'archivo.s');
    if (fileName) {
        if (!fileName.endsWith('.s')) {
            fileName += '.s';
        }
        const blob = new Blob([editorEntrada.getValue()], { type: 'text/plain' });
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }
});


document.getElementById('abrirArchivo').addEventListener('click', function () {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'glt'; 
    input.onchange = function (event) {
        const file = event.target.files[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = function (e) {
                editorEntrada.setValue(e.target.result);  
            };
            reader.readAsText(file);
        }
    };
    input.click();
});


document.getElementById('btnReportes').addEventListener('click', function () {
    const menu = document.getElementById('menuReportes');
    menu.style.display = menu.style.display === 'block' ? 'none' : 'block';
});


document.getElementById('reporteErrores').addEventListener('click', function () {
    
    window.open('/Tablaerrores.html', '_blank');
});

document.getElementById('generarArbol').addEventListener('click', async function () {
    window.open('/AST.svg', '_blank');
});


document.getElementById('generarTablaSimbolos').addEventListener('click', async function () {
    window.open('/TablaSimbolos.html', '_blank');
});


window.onclick = function(event) {
    if (!event.target.matches('#btnArchivo') && !event.target.matches('#btnReportes')) {
        const menuArchivo = document.getElementById('menuArchivo');
        const menuReportes = document.getElementById('menuReportes');
        if (menuArchivo.style.display === 'block') {
            menuArchivo.style.display = 'none';
        }
        if (menuReportes.style.display === 'block') {
            menuReportes.style.display = 'none';
        }
    }
};