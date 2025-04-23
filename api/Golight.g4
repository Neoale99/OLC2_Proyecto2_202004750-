    grammar Golight;


    program
        : contenido+
        ;



    contenido
        :   declaracion
        |   println
        |   funcionesnativas
        |   funciones
        |   secuenciascontrol
        |   sentenciastransferencia
        |   incredecre
        |   bloqueindependiente
        |   expresion
        ;

    bloqueindependiente
        :   '{' contenido+ '}'                                                         #bloque    
        ;

    funciones
        :  'func' expresion '(' ')' TIPO? '{' contenido+ '}'                                                #funcionvacia
        |  'func' expresion '(' parametros+ ')' TIPO? '{' contenido+'}'                                     #funcionconparametros 
        |  'func' '(' parametros+ ')' expresion '(' (parametros+)? ')' TIPO? '{' contenido+ '}'             #funcionanonima
        ;

    parametros
        :   (ID TIPO)+ ','?
        |   (expresion)+ ','?
        |   ID ID
        ;

    declaracion
        :   'var' ID '=' '[]' TIPO '{' valores+ '}'                                    #declaracionslicevalor
        |   'var' ID TIPO '=' expresion                                                #declaexplicitavalor
        |   'var' ID TIPO                                                              #declaexplicitanovalor                             
        |   'var' ID '[]' TIPO                                                         #declaracionslicenovalor
        |    ID AC expresion                                                           #declaracionimplicita
        |    ID (AC|'=') ('[]')+ TIPO '{'? slices+ '}'?                                #declaracionimplicitaslice
        |    ID ('[' valor ']')+ '=' expresion                                         #asignacionslicesimple
        |    expresion '.' expresion '=' expresion                                     #asignacionmetodos
        |    ID ('.' expresion+)? ('='| AC) ID '.' expresion                           #asignacionstruct
        |    expresion '=' expresion                                                   #asignacion          
        |    ID AC expresion '.' expresion '('')'                                      #asignaciondesdefuncion
        ;

    slices
        :   '{' valores+ '}' ','?
        ;

    expresion
        : valores                                                                       #valoresexpr         
        | ID    '(' parametros* ')'                                                     #llamadafuncion2                 
        | '(' expresion ')'                                                             #parentesisexpre
        | '[' expresion ']'                                                             #corchetesexpre
        | <assoc=right> op = ('!' | '-') expresion                                      #unario
        | expresion op = ('*' | '/' | '%') expresion                                    #multdivmod
        | expresion op = ('+' | '-') expresion                                          #sumres
        | expresion op = ('<' | '<=' | '>=' | '>') expresion                            #relacionales
        | expresion op = ('==' | '!=') expresion                                        #igualdad
        | expresion '&&' expresion                                                      #and
        | expresion '||' expresion                                                      #or
        | ID ('[' valor ']')+                                                           #slicesdesc
        | ID                                                                            #id              
        | valor                                                                         #valorexpr
        | funcionesnativas                                                              #nativas
        | incredecre                                                                    #incredecr      
        | ID '.' ID                                                                     #expdotexp1             
        | ID '.' expresion                                                              #expdotexp      
        | expresion '.' ID '(' expresion* ')'                                           #llamadafuncion1    
        | ID '=' expresion                                                              #asignacionfor
        ;

    println
        :   PRINT '(' expresion (',' expresion ('.' expresion)?)* ')' 
        |   PRINT '(' ')'
        ;

    funcionesnativas
        :   'strconv.Atoi' '(' CADENA ')'                                               #atoi
        |   'strconv.ParseFloat' '(' CADENA ')'                                         #parsefloat 
        |   'reflect.TypeOf' '(' ID ')'                                                 #typeof
        |   'slices.Index' '(' ID ',' valor ')'                                         #index
        |   'strings.join'  '(' ID ',' CADENA ')'                                       #join
        |   'len' '(' expresion ')'                                                     #len
        |   'append' '(' ID ',' expresion ')'                                               #append
        ;

    incredecre
        :   ID '++'                                                                     #incremento
        |   ID '--'                                                                     #decremento          
        |   ID '+=' expresion                                                           #incremento2
        |   ID '-=' expresion                                                           #decremento2                            
        ;

    secuenciascontrol
        :   if+
        |   switch+
        |   for+
        ;

    if
        : 'if' expresion+ '{' contenido+ '}'                                             #if1     
        | 'else if' expresion+ '{' contenido+ '}'                                        #elseif        
        | 'else' '{' contenido+ '}'                                                      #else
        ;

    switch
        : 'switch' expresion '{' casos+ '}'
        ;

    casos
        : 'case' expresion ':' contenido+                                                #case            
        | 'default' ':' contenido+                                                       #default
        ;

    for
        : 'for' expresion  '{' contenido+ '}'                                            #for1
        | 'for' expresion? declaracion? ';' expresion ';' expresion  '{' contenido+ '}'  #for2    
        | 'for' expresion  ',' expresion AC 'range' expresion '{' contenido+ '}'         #for3
        ;

    sentenciastransferencia
        :   'break'                                                                      #break                   
        |   'continue'                                                                   #continue                   
        |   'return' expresion*                                                          #return                   
        ;


    valores
        :   ',' valor
        |   valor
        ;

    valor
        :   ENTERO                                                                          #int
        |   FLOTANTE                                                                        #float64
        |   CADENA                                                                          #string
        |   BOOLEANO                                                                        #bool
        |   RUNE                                                                            #rune
        |   'nil'                                                                           #null
        ;

    TIPO : 'int' | 'float64' | 'string' | 'bool' | 'rune'| 'nil' ;
    AC : ':=';
    ENTERO : [0-9]+ ;
    FLOTANTE : [0-9]+ '.' [0-9]+ ;
    CADENA: '"' ('\\' [\\"nrt] | ~["\\\r\n])* '"';
    BOOLEANO: 'true' | 'false' ;
    RUNE : '\'' . '\'' ;
    WS : [ \t\r\n]+ -> skip;
    COMENTARIOSIMPLE : '//' ~[\r\n]* -> skip;
    COMENTARIOMULTILINEA : '/*' .*? '*/' -> skip;
    PRINT : 'fmt.Println' ;
    ID  : [a-zA-Z_]+[a-zA-Z_0-9]* ;


