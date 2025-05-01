using System.Collections.Generic;

public class StandardLibrary
{
    private readonly HashSet<string> UsedFunctions = new HashSet<string>();
    public void Use(string function)
    {
        UsedFunctions.Add(function);
    }

    public string GetFunctionDefinitions()
    {
        var functions = new List<string>();

        foreach (var function in UsedFunctions)
        {
            if (FunctionDefinitions.TryGetValue(function, out var definition))
            {
                functions.Add(definition);
            }
        }


        return string.Join("\n\n", functions);
    }

    private readonly static Dictionary<string, string> FunctionDefinitions = new Dictionary<string, string>
    {
        { "print_integer", @"
    //--------------------------------------------------------------
    // print_integer - Prints a signed integer to stdout
    //
    // Input:
    //   x0 - The integer value to print
    //--------------------------------------------------------------
    print_integer:
        // Save registers
        stp x29, x30, [sp, #-16]!  // Save frame pointer and link register
        stp x19, x20, [sp, #-16]!  // Save callee-saved registers
        stp x21, x22, [sp, #-16]!
        stp x23, x24, [sp, #-16]!
        stp x25, x26, [sp, #-16]!
        stp x27, x28, [sp, #-16]!
        
        // Check if number is negative
        mov x19, x0                // Save original number
        cmp x19, #0                // Compare with zero
        bge positive_number        // Branch if greater or equal to zero
        
        // Handle negative number
        mov x0, #1                 // fd = 1 (stdout)
        adr x1, minus_sign         // Address of minus sign
        mov x2, #1                 // Length = 1
        mov w8, #64                // Syscall write
        svc #0
        
        neg x19, x19               // Make number positive
        
    positive_number:
        // Prepare buffer for converting result to ASCII
        sub sp, sp, #32            // Reserve space on stack
        mov x22, sp                // x22 points to buffer
        
        // Initialize digit counter
        mov x23, #0                // Digit counter
        
        // Handle special case for zero
        cmp x19, #0
        bne convert_loop
        
        // If number is zero, just write '0'
        mov w24, #48               // ASCII '0'
        strb w24, [x22, x23]       // Store in buffer
        add x23, x23, #1           // Increment counter
        b print_result             // Skip conversion loop
        
    convert_loop:
        // Divide the number by 10
        mov x24, #10
        udiv x25, x19, x24         // x25 = x19 / 10 (quotient)
        msub x26, x25, x24, x19    // x26 = x19 - (x25 * 10) (remainder)
        
        // Convert remainder to ASCII and store in buffer
        add x26, x26, #48          // Convert to ASCII ('0' = 48)
        strb w26, [x22, x23]       // Store digit in buffer
        add x23, x23, #1           // Increment digit counter
        
        // Prepare for next iteration
        mov x19, x25               // Quotient becomes the new number
        cbnz x19, convert_loop     // If number is not zero, continue
        
        // Reverse the buffer since digits are in reverse order
        mov x27, #0                // Start index
    reverse_loop:
        sub x28, x23, x27          // x28 = length - current index
        sub x28, x28, #1           // x28 = length - current index - 1
        
        cmp x27, x28               // Compare indices
        bge print_result           // If crossed, finish reversing
        
        // Swap characters
        ldrb w24, [x22, x27]       // Load character from start
        ldrb w25, [x22, x28]       // Load character from end
        strb w25, [x22, x27]       // Store end character at start
        strb w24, [x22, x28]       // Store start character at end
        
        add x27, x27, #1           // Increment start index
        b reverse_loop             // Continue reversing
        
    print_result:
        // Add newline
        mov w24, #10               // Newline character
        strb w24, [x22, x23]       // Add to end of buffer
        add x23, x23, #1           // Increment counter
        
        // Print the result
        mov x0, #1                 // fd = 1 (stdout)
        mov x1, x22                // Buffer address
        mov x2, x23                // Buffer length
        mov w8, #64                // Syscall write
        svc #0
        
        // Clean up and restore registers
        add sp, sp, #32            // Free buffer space
        ldp x27, x28, [sp], #16    // Restore callee-saved registers
        ldp x25, x26, [sp], #16
        ldp x23, x24, [sp], #16
        ldp x21, x22, [sp], #16
        ldp x19, x20, [sp], #16
        ldp x29, x30, [sp], #16    // Restore frame pointer and link register
        ret                        // Return to caller

    minus_sign:
        .ascii ""-""               // Minus sign
        .align 4                   //align to 4 bytes"                  
        
    },

    { "print_string", @"
    //--------------------------------------------------------------
    // print_string - Prints a null-terminated string to stdout
    //
    // Input:
    //   x0 - The address of the null-terminated string to print
    //--------------------------------------------------------------
    print_string:
        // Save registers (maintain 16-byte alignment)
        stp x29, x30, [sp, #-32]!   // Save FP and LR
        mov x29, sp
        stp x19, x20, [sp, #16]     // Save temporary registers

        mov x19, x0                 // Save string pointer

    print_loop:
        ldrb w20, [x19]
        cbz w20, print_newline

        mov x0, #1
        mov x1, x19
        mov x2, #1
        mov x8, #64
        svc #0

        add x19, x19, #1
        b print_loop

    print_newline:
        mov x0, #1
        adr x1, newline_char
        mov x2, #1
        mov x8, #64
        svc #0

        ldp x19, x20, [sp, #16]     // Recupera temporales
        ldp x29, x30, [sp], #32     // Recupera FP y LR y libera los 32 bytes
        ret
    " },
    { "modulo", @"
    //--------------------------------------------------------------
    // modulo - Computes a % b (unsigned)
    //
    // Input:
    //   x0 - Dividend (a)
    //   x1 - Divisor (b)
    // Output:
    //   x0 - Remainder (a % b)
    //--------------------------------------------------------------
    modulo:
        // Save registers (guarantees 16-byte alignment)
        stp x29, x30, [sp, #-32]!  // Allocate 32 bytes (maintains alignment)
        stp x19, x20, [sp, #16]     // Store additional registers at proper offset
        
        // Move arguments to saved registers
        mov x19, x0                 // x19 = a
        mov x20, x1                 // x20 = b
        
        // Check for division by zero
        cbz x20, 1f                 // Forward local label (aligned automatically)
        
        // Compute modulo using UDIV and MSUB
        udiv x2, x19, x20           // x2 = a / b
        msub x0, x2, x20, x19       // x0 = a - (a/b)*b
        
    modulo_return:
        // Restore registers and return
        ldp x19, x20, [sp, #16]
        ldp x29, x30, [sp], #32
        ret

        // Local label for error handling (automatically aligned)
    1:
        // Handle division by zero
        mov x0, #0
        mov x1, #1                  // Error flag
        b modulo_return
    "} ,
 { "print_float_asm", @"
    //--------------------------------------------------------------
    // print_float_asm - Imprime un número de punto flotante 
    //
    // Input:
    //   d0 - El valor flotante a imprimir
    //--------------------------------------------------------------
    print_float_asm:
        // Guardar registros
        stp x29, x30, [sp, #-16]!
        stp x19, x20, [sp, #-16]!
        stp x21, x22, [sp, #-16]!
        stp x23, x24, [sp, #-16]!
        stp d8, d9, [sp, #-16]!
        
        // Guardar el valor original
        fmov d8, d0
        
        // Comprobar si el número es negativo
        fmov x0, d0
        lsr x1, x0, #63         // Obtener el bit de signo (bit 63)
        cbz x1, 1f              // Si es 0, el número es positivo (usamos etiqueta local)
        
        // Imprimir signo negativo si es necesario
        mov x0, #1              // fd stdout
        adr x1, float_minus     // dirección del signo menos
        mov x2, #1              // longitud 1
        mov x8, #64             // syscall write
        svc #0
        
        // Hacer el valor positivo para procesarlo
        fneg d8, d8
        
    1:  // float_positive
        .align 4                // Asegurar alineación
        // Extraer la parte entera
        fcvtzs x19, d8          // Convertir a entero con redondeo hacia cero (truncar)
        scvtf d9, x19           // Convertir de nuevo a flotante para obtener la parte entera exacta
        fsub d8, d8, d9         // d8 ahora tiene solo la parte fraccionaria
        
        // Imprimir la parte entera usando print_integer_local que es una versión modificada
        // para funcionar con nuestra implementación
        mov x0, x19
        
        // Esta es la llamada que imprime la parte entera
        // Si está fallando, debemos verificar que esta subrutina funcione correctamente
        bl int_print_part
        
        // Imprimir el punto decimal
        mov x0, #1              // fd stdout
        adr x1, float_point     // dirección del punto
        mov x2, #1              // longitud 1
        mov x8, #64             // syscall write
        svc #0
        
        // Procesar la parte fraccionaria
        mov x23, #6             // Precisión (6 dígitos después del punto)
        mov x21, #10            // Base decimal
        fcvt s8, d8             // Convertir a float para mejor precisión en operaciones repetidas
        
    3:  // print_fraction
        .align 4                // Asegurar alineación
        // Multiplicar la parte fraccionaria por 10
        fmov s9, #10.0
        fmul s8, s8, s9
        
        // Extraer el dígito
        fcvtzs x22, s8
        scvtf s9, x22
        fsub s8, s8, s9
        
        // Imprimir el dígito
        add x22, x22, #48       // Convertir a ASCII
        // Empujar al stack y usarlo como buffer
        sub sp, sp, #16
        strb w22, [sp]
        
        mov x0, #1              // fd stdout
        mov x1, sp              // dirección del dígito
        mov x2, #1              // longitud 1
        mov x8, #64             // syscall write
        svc #0
        
        add sp, sp, #16         // Restaurar stack
        
        subs x23, x23, #1       // Decrementar contador de precisión
        bne 3b                  // Si no es cero, continuar (b hacia atrás a la etiqueta 3)
        
        // Imprimir nueva línea
        mov x0, #1              // fd stdout
        adr x1, float_nl        // dirección del newline
        mov x2, #1              // longitud 1
        mov x8, #64             // syscall write
        svc #0
        
        // Restaurar registros y retornar
        ldp d8, d9, [sp], #16
        ldp x23, x24, [sp], #16
        ldp x21, x22, [sp], #16
        ldp x19, x20, [sp], #16
        ldp x29, x30, [sp], #16
        ret

    // Subrutina para imprimir solo la parte entera de un número
    int_print_part:
        .align 4                // Asegurar alineación
        stp x29, x30, [sp, #-16]!
        
        // Reservar espacio para el buffer de dígitos (32 bytes)
        sub sp, sp, #32
        mov x1, sp
        
        // Si el entero es cero, manejo especial
        cbnz x0, 4f             // Ir a int_not_zero si no es cero
        mov w2, #48             // ASCII '0'
        strb w2, [x1]
        mov x2, #1              // Longitud 1
        b 6f                    // Ir a int_print
        
    4:  // int_not_zero
        .align 4                // Asegurar alineación
        mov x2, #0              // Contador de dígitos
        mov x21, #10            // Base 10 para divisiones
        
    5:  // int_loop
        .align 4                // Asegurar alineación
        // Dividir por 10 para obtener el dígito
        udiv x3, x0, x21        // x3 = x0 / 10
        msub x4, x3, x21, x0    // x4 = x0 - (x3 * 10) = x0 % 10
        
        // Convertir a ASCII y almacenar
        add w4, w4, #48         // Convertir a ASCII
        strb w4, [x1, x2]       // Almacenar en buffer
        add x2, x2, #1          // Incrementar contador
        
        // Continuar con el cociente
        mov x0, x3              // Siguiente número a procesar
        cbnz x0, 5b             // Si no es cero, continuar (b hacia atrás a la etiqueta 5)
        
        // Invertir los dígitos (están en orden inverso)
        mov x3, #0              // Índice inicial
    7:  // int_reverse
        .align 4                // Asegurar alineación
        cmp x3, x2, lsr #1      // Comparar con la mitad de la longitud
        bge 6f                  // Si ya llegamos a la mitad, terminar (ir a int_print)
        
        // Intercambiar dígitos
        sub x4, x2, x3          // x4 = longitud - índice
        sub x4, x4, #1          // x4 = longitud - índice - 1
        
        ldrb w5, [x1, x3]       // Cargar dígito del inicio
        ldrb w6, [x1, x4]       // Cargar dígito del final
        strb w6, [x1, x3]       // Guardar dígito del final en el inicio
        strb w5, [x1, x4]       // Guardar dígito del inicio en el final
        
        add x3, x3, #1          // Incrementar índice
        b 7b                    // Volver al bucle reverse (b hacia atrás a la etiqueta 7)
        
    6:  // int_print
        .align 4                // Asegurar alineación
        // Imprimir los dígitos (sin agregar nueva línea)
        mov x0, #1              // fd stdout
        // x1 ya tiene la dirección
        // x2 ya tiene la longitud
        mov x8, #64             // syscall write
        svc #0
        
        // Liberar buffer y retornar
        add sp, sp, #32
        ldp x29, x30, [sp], #16
        ret

    .align 4                    // Asegurar alineación
    float_minus:
        .ascii ""-""
        .align 4                // Asegurar alineación
    float_point:
        .ascii "".""
        .align 4                // Asegurar alineación
    float_nl:
        .ascii ""\n""
        .align 4                // Asegurar alineación
    "}
    };
}