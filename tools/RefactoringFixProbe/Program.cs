using CodeAssist.Core;
using System.Text.Json;
var outputs=new Dictionary<string,string>();
string Insert(string text,int pos,string code,string owner=null){var e=CppClassTransforms.InsertMember(text,pos,code,owner);return text.Insert(e.Start,e.Text);}
outputs["setter"]="class EncapAudit {\n"+CppRefactoring.EncapsulateField("int value;","Value")+"};\nint main(){ EncapAudit a; a.SetValue(7); return a.GetValue()==7 ? 0:1; }\n";
var member="class AddMemberAudit {\npublic:\n};\n";outputs["member"]=Insert(member,8,"int addedField;")+"int main(){ AddMemberAudit a; a.addedField=7; return a.addedField==7 ? 0:1; }\n";
var decl="class DeclAudit {};\nint DeclAudit::Value() { return 7; }\n";outputs["declaration"]=Insert(decl,decl.IndexOf("Value"),CppRefactoring.CreateDeclaration("int DeclAudit::Value() { return 7; }"),"DeclAudit")+"int main(){ DeclAudit a; return a.Value()==7 ? 0:1; }\n";
var derived="class Base { public: virtual int Value()=0; virtual ~Base()=default; };\nclass Derived:public Base {};\n";outputs["override"]=Insert(derived,derived.IndexOf("Derived")+2,CppRefactoring.CreateImplementations("virtual int Value() = 0;","",overrides:true,bodyTemplate:"    return 7;"))+"int main(){ Derived d; Base& b=d; return b.Value()==7 ? 0:1; }\n";
outputs["header"]="#pragma once\nclass HeaderAudit { public: int Value(); };\n"+CppClassTransforms.HeaderSafeImplementation(CppRefactoring.CreateImplementation("int Value();","HeaderAudit",bodyTemplate:"    return 7;"));
Directory.CreateDirectory(args[0]);File.WriteAllText(Path.Combine(args[0],"generated.json"),JsonSerializer.Serialize(outputs,new JsonSerializerOptions{WriteIndented=true}));
