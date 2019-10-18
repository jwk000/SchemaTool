﻿//this file is generated by codedump tool at ${META.DATETIME} do NOT edit it !

#include "${Meta.name}_config.h"

bool ${Meta.Name}Config::Init()
{
	m_reader.Init(this, NULL, NULL);
@{FOREACH(Class IN ${Meta.ClassList})}
@{IF(${Class.IsDump}==true)}
	m_reader.Register("${Class.Name}", "${Class.CsvPath}");
@{END_IF}
@{END_FOREACH}

	return true;
}

void ${Meta.Name}Config::Update()
{
	m_reader.Update();
}

bool ${Meta.Name}Config::ReadBytes(const char *key, BytesParser *parser, bool init)
{
	if (parser == nullptr)
	{
		return false;
	}
	std::string str_key(key);
	@{FOREACH(Class IN ${Meta.ClassList})}
	@{IF(${Class.IsDump}==true)}
	if (str_key == "${Class.Name}")
	{
		return Load${Class.Name}(*parser, init);
	}
	@{END_IF}
	@{END_FOREACH}

	return true;
}

@{FOREACH(Class IN ${Meta.ClassList})}
@{IF(${Class.IsDump}==true)}
bool ${Meta.Name}Config::Load${Class.Name}(BytesParser &parser, bool init)
{
	m_${Class.Name}.clear();
	while (parser.MoveNext())
	{
		${Class.Name} element;
		@{FOREACH(Field IN ${Class.FieldList})}
		@{SWITCH(${Field.MetaType})}
		@{CASE(int|enum|bool|string|float|double)}
		if (!parser.Get("${Field.Name}", element.${Field.Name})){
			lightAssert(!"${Field.Name}");
			return false;
		}
		@{CASE(object)}
		{
			std::string line;
			if (!parser.Get("${Field.Name}", line))
			{
				return false;
			}
			auto tokens = split_string(line, ",");
			@{SET(fieldobj=${Field.Object})}
			@{END_SET}
			@{FOREACH(field IN ${fieldobj.FieldList})}
			@{SWITCH(${field.MetaType})}
			${CASE(string)}
			element.${Field.Name}.${field.Name} = tokens[${ForIndex}];
			@{CASE(int|bool)} 
			element.${Field.Name}.${field.Name} = std::stoi(tokens[${ForIndex}]);
			@{CASE(enum)} 
			element.${Field.Name}.${field.Name} = (${field.CppType})std::stoi(tokens[${ForIndex}]);
			@{CASE(float)}
			element.${Field.Name}.${field.Name} = std::stof(tokens[${ForIndex}]);
			@{END_SWITCH}
			@{END_FOREACH}
		}
		@{CASE(array)}
		{
			std::string line;
			if (!parser.Get("${Field.Name}", line))
			{
				return false;
			}
			auto tokens = split_string(line, "${Field.ArrayDelimiter}");
			for (auto token : tokens)
			{
			@{SWITCH(${Field.InnerMetaType})}
			@{CASE(string)}
				element.${Field.Name}.push_back(token);
			@{CASE(int|bool)}
				element.${Field.Name}.push_back(std::stoi(token));
			@{CASE(enum)}
				element.${Field.Name}.push_back((${Field.InnerCppType})std::stoi(token));
			@{CASE(float)}
				element.${Field.Name}.push_back(std::stof(token));
			@{CASE(object)}
				${Field.InnerCppType} ee;
				auto ss = split_string(token, ",");
				@{SET(fieldobj=${Field.Object})}
				@{END_SET}
				@{FOREACH(field IN ${fieldobj.FieldList})}
				@{SWITCH(${field.MetaType})}
				${CASE(string)}
				ee.${field.Name} = ss[${ForIndex}];
				@{CASE(int|bool|enum)} 
				ee.${field.Name} = std::stoi(ss[${ForIndex}]);
				@{CASE(float)}
				ee.${field.Name} = std::stof(ss[${ForIndex}]);
				@{END_SWITCH}
				@{END_FOREACH}
				element.${Field.Name}.push_back(ee);
			@{END_SWITCH}
			}
		}
		@{END_SWITCH}
		@{END_FOREACH}
		m_${Class.Name}[element.${Class.KeyFieldName}] = element;
	}

	return true;
}
@{END_IF}
@{END_FOREACH}