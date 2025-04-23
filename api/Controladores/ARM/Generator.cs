using System.Text;

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
        instruccionesARM.Add($"DIV {rd}, {rs1}, {rs2}");
    }
    public void addi (string rd, string rs1, string imm){
        instruccionesARM.Add($"ADDI {rd}, {rs1}, #{imm}");
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

    public void comentario(string comentario){
        instruccionesARM.Add($"// {comentario}");
    }
    public override string ToString(){
        var sb = new StringBuilder();
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