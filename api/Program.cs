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
        var salidaerror = "";
        try {
            //Visitor semantico
            var inputStreamsemantico = new AntlrInputStream(textoEntrada);
            var lexersemantico = new GolightSemanticoLexer(inputStreamsemantico);
            var tokenStreamsemantico = new CommonTokenStream(lexersemantico);
            var parsersemantico = new GolightSemanticoParser(tokenStreamsemantico);
            var errorListenersemantico = new Utilssemantico.RawErrorListener();
            var lexerErrorListenersemantico = new Utilssemantico.LexerErrorListener();
            parsersemantico.RemoveErrorListeners();
            parsersemantico.AddErrorListener(errorListenersemantico);    
            lexersemantico.RemoveErrorListeners();
            lexersemantico.AddErrorListener(lexerErrorListenersemantico);
            var treesemantico = parsersemantico.program();



            var erroressemantico = new List<(string Tipo, string Linea, string Columna, string Descripcion)>();
            erroressemantico.AddRange(lexerErrorListenersemantico.Errors);
            erroressemantico.AddRange(errorListenersemantico.Errors);
            var visitorsemantico = new Visitorsemantico(); 
            visitorsemantico.Visit(treesemantico); 
            var semanticError2 = new SemanticError("", null); 
            var semanticErrors2 = semanticError2.GetErrors(); 
            erroressemantico.AddRange(semanticErrors2); 
            if (erroressemantico.Count > 0)
            {
                var htmlErrores = Utilssemantico.GenerarTablaErrores(erroressemantico);
                File.WriteAllText("wwwroot/Tablaerrores.html", htmlErrores);

            }
            string dotContent = Utilssemantico.dot(treesemantico, parsersemantico);

            File.WriteAllText("wwwroot/AST.dot", dotContent);

            string svgFilePath = "wwwroot/AST.svg";
            Utilssemantico.Svgdot(dotContent, svgFilePath);

            //Elementos tras el semantico
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


        //string dotContent = Utils.dot(tree, parser);

        //File.WriteAllText("wwwroot/AST.dot", dotContent);

        //string svgFilePath = "wwwroot/AST.svg";
        //Utils.Svgdot(dotContent, svgFilePath);

        

        //Console.WriteLine("salida: " + salida);
        salidaerror = visitor.codigo.ToString();
        return Results.Text(visitor.codigo.ToString(), "text/plain");
        
        }
        catch (Exception ex)
        {
            return Results.Text(salidaerror, "text/plain");
        } 
    } 
});



app.Run();

