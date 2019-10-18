return {
{% for row in info.Rows -%}
[{{ row[info.KeyIndex] }}]={ 
{% for field in info.Fields -%}
{% if field.IsString == true -%}{{ field.Name }}="{{ row[field.Index] }}",{% else -%}{{ field.Name }}={{ row[field.Index] }},{% endif -%}
{% endfor -%} },
{% endfor -%}
}