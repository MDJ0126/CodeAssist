// @category UEAssist.ReverseEngineering
import ghidra.app.script.GhidraScript;
import ghidra.app.decompiler.DecompInterface;
import ghidra.app.decompiler.DecompileResults;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.FunctionIterator;
import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import java.nio.file.*;
import java.nio.charset.StandardCharsets;
import java.util.*;

public class ExportVaFunctions extends GhidraScript {
    @Override public void run() throws Exception {
        String[] args = getScriptArgs();
        if (args.length != 2) throw new IllegalArgumentException("Expected target RVA list and output directory");
        Path output=Paths.get(args[1]); Files.createDirectories(output);
        Gson json=new GsonBuilder().setPrettyPrinting().create();
        List<Map<String,Object>> inventory=new ArrayList<>();
        FunctionIterator iterator=currentProgram.getFunctionManager().getFunctions(true);
        while(iterator.hasNext()) {
            monitor.checkCancelled(); Function f=iterator.next();
            Map<String,Object> row=new LinkedHashMap<>();
            row.put("rva",f.getEntryPoint().subtract(currentProgram.getImageBase()));
            row.put("name",f.getName()); row.put("external",f.isExternal());
            row.put("thunk",f.isThunk()); row.put("body_address_count",f.getBody().getNumAddresses());
            row.put("prototype",f.getPrototypeString(false,false)); inventory.add(row);
        }
        Files.writeString(output.resolve("function-inventory.json"),json.toJson(inventory),StandardCharsets.UTF_8);
        LinkedHashSet<Function> targets=new LinkedHashSet<>();
        for(String line:Files.readAllLines(Paths.get(args[0]),StandardCharsets.UTF_8)) {
            if(line.isBlank() || line.startsWith("#")) continue;
            Address address=currentProgram.getImageBase().add(Long.parseUnsignedLong(line.trim().replace("0x",""),16));
            Function function=currentProgram.getFunctionManager().getFunctionContaining(address);
            if(function==null && currentProgram.getMemory().getBlock(address)!=null && currentProgram.getMemory().getBlock(address).isExecute()) {
                disassemble(address);
                function=createFunction(address,null);
                if(function!=null) println("CREATED_TARGET_FUNCTION_RVA "+line);
            }
            if(function!=null) targets.add(function); else println("UNRESOLVED_FUNCTION_RVA "+line);
        }
        List<Function> roots=new ArrayList<>(targets);
        for(Function root:roots) {
            for(Function callee:root.getCalledFunctions(monitor)) {
                if(!callee.isExternal() && !callee.isThunk() && targets.size()<180) targets.add(callee);
            }
        }
        DecompInterface decompiler=new DecompInterface();
        decompiler.openProgram(currentProgram);
        List<Map<String,Object>> results=new ArrayList<>();
        try {
            for(Function function:targets) {
                monitor.checkCancelled();
                long rva=function.getEntryPoint().subtract(currentProgram.getImageBase());
                DecompileResults result=decompiler.decompileFunction(function,60,monitor);
                Map<String,Object> row=new LinkedHashMap<>();
                row.put("rva",rva); row.put("name",function.getName());
                row.put("root",roots.contains(function)); row.put("completed",result.decompileCompleted());
                row.put("error",result.getErrorMessage());
                List<Long> calls=new ArrayList<>();
                for(Function callee:function.getCalledFunctions(monitor))
                    if(!callee.isExternal())calls.add(callee.getEntryPoint().subtract(currentProgram.getImageBase()));
                row.put("direct_callees",calls);
                if(result.decompileCompleted()) {
                    String filename=String.format("rva-%08x.c.txt",rva);
                    Files.writeString(output.resolve(filename),result.getDecompiledFunction().getC(),StandardCharsets.UTF_8);
                    row.put("artifact",filename);
                }
                results.add(row); println("DECOMPILE_RVA "+Long.toHexString(rva)+" "+result.decompileCompleted());
            }
        } finally {decompiler.dispose();}
        Files.writeString(output.resolve("decompilation-results.json"),json.toJson(results),StandardCharsets.UTF_8);
        println("EXPORTED_FUNCTIONS "+inventory.size()+" DECOMPILED_TARGETS "+results.size());
    }
}
