using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Text;
using Antlr4.Runtime;
using analyzer;
using Antlr4.Runtime.Tree;
using System.Diagnostics;
using errores;

var builder = WebApplication.CreateBuilder();
builder.Services.AddSingleton<IWebHostEnvironment>(builder.Environment);

var app = builder.Build();

app.UseDefaultFiles(); // Redirige a index.html si se accede a la raíz
app.UseStaticFiles(); // Sirve archivos estáticos como HTML, CSS, JS, etc.

// Redirigir todas las solicitudes a index.html
app.MapFallbackToFile("index.html");

app.UseHttpsRedirection();


app.MapPost("/Entrada", async (HttpContext context) =>
{
    
    using (var reader = new StreamReader(context.Request.Body))
    {
        var textoEntrada = await reader.ReadToEndAsync();
        

        var inputStream = new AntlrInputStream(textoEntrada);
        var lexer = new GolightLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new GolightParser(tokenStream);
        var errorListener = new Utils.RawErrorListener();
        var lexerErrorListener = new Utils.LexerErrorListener();
        parser.RemoveErrorListeners();
        parser.AddErrorListener(errorListener);    
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(lexerErrorListener);
        var tree = parser.program();

        var errores = new List<(string Tipo, string Linea, string Columna, string Descripcion)>();
        errores.AddRange(lexerErrorListener.Errors);
        errores.AddRange(errorListener.Errors);


        var visitor = new Visitor(); 
        visitor.Visit(tree); 

        var semanticError = new SemanticError("", null); 
        var semanticErrors = semanticError.GetErrors(); 
        errores.AddRange(semanticErrors); 
        if (errores.Count > 0)
        {
            var htmlErrores = Utils.GenerarTablaErrores(errores);
            File.WriteAllText("wwwroot/Tablaerrores.html", htmlErrores);
            return Results.Ok("Errores encontrados.");
        }

        string salida = "Lectura exitosa";
        //string dotContent = Utils.dot(tree, parser);

        //File.WriteAllText("wwwroot/AST.dot", dotContent);

        //string svgFilePath = "wwwroot/AST.svg";
        //Utils.Svgdot(dotContent, svgFilePath);

        

        //Console.WriteLine("salida: " + salida);
        return Results.Text(visitor.codigo.ToString(), "text/plain");
    } 
});



app.Run();

