lexer grammar schemaLexer;

/*
 * Lexer Rules
 */
WS : [ \t\n\r]+ -> skip;

LINE_COMMENT:'//' .*? NEWLINE -> channel(HIDDEN);

NEWLINE:'\r'? '\n' ;

NUM: [0-9]+;


DOT:'.';
COMMA:',';
COLON:':';
LEFT_BRACKET:'(';
RIGHT_BRACKET:')';
AT:'@';

DELIMITER: '|'|';'|'-' ;
OP:EQ|NE|BT|BE|LT|LE;

EQ:'==';
NE:'!=';
BT:'>';
BE:'>=';
LT:'<';
LE:'<=';

SCHEMA:'schema';
ENUM:'enum';
BEGIN:'{';
END:'}';
ARRAY_BEGIN: '[' ;
ARRAY_END:']';

PRIME_TYPE:'int'|'uint'|'string'|'bool'|'float'|'double';

RANGE:'range';
FLAG:'flag';
KEY:'key';
NULLABLE:'nullable';
DEFAULT:'default';
REF:'ref';
MAP:'map';
BIND:'bind';
TARGET:'target';
DUMP:'dump';

ID : [a-zA-Z_][a-zA-Z0-9_]* ; 
