using System.Text;

//public class StackObject {
//    public emum StackObjectType {Int, Float, String, Bool}
//    public StackObjectType Type {get; set;}
//    public int Largo {get; set;}
//    public int Profundidad {get; set;}
//    public string? Id {get; set;}
//}
public class GeneradorARM{

    private readonly List<string> instruccionesARM = new List<string>();
    private readonly StandardLibrary stdlib = new StandardLibrary();
    public void add (string rd, string rs1, string rs2){
        instruccionesARM.Add($"ADD {rd}, {rs1}, {rs2}");
    }
    public void sub (string rd, string rs1, string rs2){
        instruccionesARM.Add($"SUB {rd}, {rs1}, {rs2}");
    }
    public void mul (string rd, string rs1, string rs2){
        instruccionesARM.Add($"MUL {rd}, {rs1}, {rs2}");
    }
    public void div (string rd, string rs1, string rs2){
        instruccionesARM.Add($"SDIV {rd}, {rs1}, {rs2}");
    }
    public void addi (string rd, string rs1, string imm){
        instruccionesARM.Add($"ADDI {rd}, {rs1}, #{imm}");
    }
    public void mod(string rd, string rs1, string rs2){
        stdlib.Use("modulo");
        instruccionesARM.Add($"MOV X0, {rs1}");  
        instruccionesARM.Add($"MOV X1, {rs2}");  

        instruccionesARM.Add($"BL modulo");
    }
    //Operaciones en memoria
    public void str (string rs, string rd, string offset){
        instruccionesARM.Add($"STR {rs}, [{rd}, #{offset}]");
    }
    public void ldr (string rd, string rs, string offset){
        instruccionesARM.Add($"LDR {rd}, [{rs}, #{offset}]");
    }
    public void mov (string rd, int imm){
        instruccionesARM.Add($"MOV {rd}, #{imm}");
    }
    public void push (string rs){
        instruccionesARM.Add($"STR {rs}, [SP, #-16]!");
    }
    public void pop (string rd){
        instruccionesARM.Add($"LDR {rd}, [SP], #16");
    }
    public void svc (){
        instruccionesARM.Add($"SVC #0");
    }
    public void finalizar(){
        mov(Registers.x0,0); //Syscall para salir
        mov(Registers.x8, 93); //Syscall para salir
        svc(); //Hacemos Syscall
    }

    public void Printint(string rs){
        stdlib.Use("print_integer");
        instruccionesARM.Add($"MOV X0, {rs}");
        instruccionesARM.Add($"BL print_integer");
    }

    public void Printstr(string text)
    {
        stdlib.Use("print_string");
        
        // 1. Calcular espacio necesario (alineado a 16 bytes)
        int length = text.Length + 1; // +1 para el null terminator
        int alignedSize = ((length + 15) / 16) * 16;
        
        // 2. Reservar espacio en el stack
        instruccionesARM.Add($"SUB SP, SP, #{alignedSize}");
        instruccionesARM.Add($"MOV X0, SP"); // Guardar dirección base
        
        for (int i = 0; i < text.Length; i++)
        {
            int offset = i;
            char c = text[i];
            instruccionesARM.Add($"MOV W1, #{(int)c}");
            instruccionesARM.Add($"STRB W1, [X0, #{offset}]");
        }
        
        // 4. Añadir null terminator
        instruccionesARM.Add($"MOV W1, #0");
        instruccionesARM.Add($"STRB W1, [X0, #{text.Length}]");
        
        instruccionesARM.Add($"BL print_string");
        
        instruccionesARM.Add($"ADD SP, SP, #{alignedSize}");
    }

    public void comentario(string comentario){
        instruccionesARM.Add($"// {comentario}");
    }
    public override string ToString(){
        var sb = new StringBuilder();
        sb.AppendLine(".data");
        sb.AppendLine("newline_char:");
        sb.AppendLine(".ascii \"\\n\""); 
        sb.AppendLine(".text");
        sb.AppendLine(".global _start");
        sb.AppendLine("_start:");
        finalizar();
        foreach (var instruccion in instruccionesARM){
            sb.AppendLine(instruccion);
        }
        sb.AppendLine("\n//Librerias estandar");
        sb.AppendLine(stdlib.GetFunctionDefinitions());
        return sb.ToString();
    }

}