from foreground106 import *
cases=[('open-only','void MisOpen107() {\r\n    return;\r\n',lambda t:t.index('return')),('nested-open','void MisNested107() {\r\n    if(true) {\r\n        return;\r\n}\r\n',lambda t:t.index('return')),('wrong-close','void MisWrong107() {\r\n    if(true) {\r\n        return;\r\n)\r\n}\r\n',lambda t:t.index('return')),('paren-mismatch','void MisParen107() {\r\n    int value = (1 + 2;\r\n}\r\n',lambda t:t.index('1 +'))]
for label,text,where in cases:
 setup('VA',label+'107',text,where(text));physical('VA',label+'-focus','focus');module('VA','arm-markers105',windowTitle='');physical('VA',label+'-move','keys',keys='RIGHT');time.sleep(.6);observed('VA',label)
