parser grammar schemaParser;

options { tokenVocab=schemaLexer; }
/*
 * Parser Rules
 */

prog :stat+; 

stat:enum|schema;

enum: ID COLON ENUM desc* BEGIN enum_field (COMMA enum_field)* COMMA? END;

enum_field: ID COLON NUM ;

schema: ID COLON SCHEMA desc* BEGIN schema_field (COMMA schema_field)* COMMA? END;

schema_field : ID COLON field_type desc* ;

field_type:prime_type
		   |object_type
		   |array_type
		   |enum_type
			;

prime_type: PRIME_TYPE;
enum_type: ID;
object_type:BEGIN field_type DELIMITER? END ;
array_type: ARRAY_BEGIN field_type DELIMITER? ARRAY_END;


desc:range_desc
	|flag_desc
	|key_desc
	|nullable_desc
	|default_desc
	|target_desc
	|ref_desc
	|map_desc
	 |dump_desc
	 ;

range_desc:AT RANGE LEFT_BRACKET MIN=NUM? COMMA MAX=NUM? RIGHT_BRACKET;
flag_desc:AT FLAG;
key_desc:AT KEY;
nullable_desc:AT NULLABLE LEFT_BRACKET COND=ID OP VALUE=.*? RIGHT_BRACKET;
default_desc:AT DEFAULT LEFT_BRACKET VALUE=.*? RIGHT_BRACKET ;
target_desc:AT TARGET LEFT_BRACKET ID (COMMA ID)* RIGHT_BRACKET ;
ref_desc:AT REF LEFT_BRACKET TABLE=ID DOT FIELD=ID (COMMA COND=ID OP VALUE=.*?)? RIGHT_BRACKET ;
bind_desc:AT BIND LEFT_BRACKET FA=ID OP VA=.*? COMMA FB=ID OP VB=.*? RIGHT_BRACKET;
map_desc:AT MAP LEFT_BRACKET FA=ID COMMA TABLE=ID DOT FIELD=ID RIGHT_BRACKET;
dump_desc:AT DUMP;