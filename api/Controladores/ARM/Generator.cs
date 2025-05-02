using System.Text;

public class StackObject
{
    public enum StackObjectType    {Int,Float,String,Rune,Bool}
    public StackObjectType Type { get; set; }
    public string Name { get; set; }
    public int Length { get; set; }
    public int Depth { get; set; }
}
public class GeneradorARM{

    private readonly List<string> instruccionesARM = new List<string>();
    private readonly StandardLibrary stdlib = new StandardLibrary();
    private List<StackObject> stack = new List<StackObject>();
    private int depth = 0;

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
    public void addFloat(string rd, string rs1, string rs2) {
        instruccionesARM.Add("// Suma de flotantes");
        instruccionesARM.Add($"FADD {rd}, {rs1}, {rs2}");
    }

    public void subFloat(string rd, string rs1, string rs2) {
        instruccionesARM.Add("// Resta de flotantes");
        instruccionesARM.Add($"FSUB {rd}, {rs1}, {rs2}");
    }

    public void mulFloat(string rd, string rs1, string rs2) {
        instruccionesARM.Add("// Multiplicación de flotantes");
        instruccionesARM.Add($"FMUL {rd}, {rs1}, {rs2}");
    }

    public void divFloat(string rd, string rs1, string rs2) {
        instruccionesARM.Add("// División de flotantes");
        instruccionesARM.Add($"FDIV {rd}, {rs1}, {rs2}");
    }

    public void pushFloat() {
        instruccionesARM.Add($"STR d0, [SP, #-16]!");
    }

    public void popFloat() {
        instruccionesARM.Add($"LDR d1, [SP], #16");
    }

    public void IntToFloat() {
        comentario("Convirtiendo entero a flotante");
        instruccionesARM.Add($"SCVTF d0, x0");
    }

    public void LoadIntToFloat(string reg) {
        comentario($"Cargando {reg} como flotante");
        instruccionesARM.Add($"SCVTF d1, {reg}");
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

    public void PrintNewLine()
    {
        stdlib.Use("print_newline2");
        instruccionesARM.Add($"BL print_newline2");
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

//Implemenacion del flotante
    public void LoadFloatBits(double value)
    {
        ulong bits = BitConverter.DoubleToUInt64Bits(value);
        comentario($"Cargando valor flotante {value} (bits: 0x{bits:X16})");
        instruccionesARM.Add($"MOVZ x0, #0x{bits & 0xFFFF:X4}");
        instruccionesARM.Add($"MOVK x0, #0x{(bits >> 16) & 0xFFFF:X4}, LSL #16");
        instruccionesARM.Add($"MOVK x0, #0x{(bits >> 32) & 0xFFFF:X4}, LSL #32");
        instruccionesARM.Add($"MOVK x0, #0x{(bits >> 48) & 0xFFFF:X4}, LSL #48");
        instruccionesARM.Add($"FMOV d0, x0");
        pushFloat();
    }
    public void PrintFloat()
    {
        stdlib.Use("print_float_asm");
        instruccionesARM.Add($"BL print_float_asm");
    }
    public void SwapFloats() {
        instruccionesARM.Add("FMOV d2, d0");  // Backup d0
        instruccionesARM.Add("FMOV d0, d1");  // Move d1 to d0
        instruccionesARM.Add("FMOV d1, d2");  // Restore from backup to d1
    }

    public void comentario(string comentario){
        instruccionesARM.Add($"// {comentario}");
    }
    public override string ToString(){
        var sb = new StringBuilder();
        sb.AppendLine(".data");
        sb.AppendLine("newline_char:");
        sb.AppendLine(".ascii \"\\n\"");
        sb.AppendLine("space_char:");
        sb.AppendLine(".ascii \" \""); 
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
    // Metodos para ciclos 
    public void cmp(string rs1, int value)
    {
        instruccionesARM.Add($"CMP {rs1}, #{value}");
    }

    public void cmp(string rs1, string rs2)
    {
        instruccionesARM.Add($"CMP {rs1}, {rs2}");
    }

    public void b(string etiqueta)
    {
        instruccionesARM.Add($"B {etiqueta}");
    }

    public void bne(string etiqueta)
    {
        instruccionesARM.Add($"BNE {etiqueta}");
    }

    public void beq(string etiqueta)
    {
        instruccionesARM.Add($"BEQ {etiqueta}");
    }

    public void etiqueta(string nombre)
    {
        instruccionesARM.Add($"{nombre}:");
    }
}