using System;
using System.Collections.Generic;
namespace CodeAssist.ParityRunner
{
    internal static class FullSuiteCommands
    {
        internal static readonly HashSet<string> AllowedVa=new HashSet<string>(StringComparer.Ordinal) {
            "VAssistX.Options","VAssistX.RefactorRename","VAssistX.RefactorChangeSignature",
            "VAssistX.RefactorConvertBetweenPointerAndInstance","VAssistX.RefactorEncapsulateField",
            "VAssistX.RefactorExtractMethod","VAssistX.RefactorIntroduceVariable",
            "VAssistX.RefactorMoveImplementationToHeaderFile","VAssistX.RefactorMoveImplementationToSourceFile",
            "VAssistX.RefactorMoveMethodImplementationsToSourceFile","VAssistX.RefactorMoveSelectionToNewFile",
            "VAssistX.RefactorRenameFiles","VAssistX.RefactorSimplifyInstanceDeclaration",
            "VAssistX.RefactorAddForwardDeclaration","VAssistX.RefactorAddMember","VAssistX.RefactorAddMissingCaseStatements",
            "VAssistX.RefactorAddSimilarMember","VAssistX.RefactorAddUsingDirective","VAssistX.RefactorAddIncludeDirective",
            "VAssistX.BracesToggle","VAssistX.RefactorCreateDeclaration","VAssistX.RefactorCreateFile",
            "VAssistX.RefactorCreateFromUsage","VAssistX.RefactorCreateImplementation","VAssistX.RefactorCreateMethodImplementations",
            "VAssistX.RefactorDocumentMethod","VAssistX.RefactorImplementInterface","VAssistX.OpenFileInSolutionDialog",
            "VAssistX.FindSymbolDialog","VAssistX.GotoMember","VAssistX.GotoRelated","VAssistX.Paste",
            "VAssistX.SortSelectedLines","VAssistX.VaSnippetInsert","VAssistX.CreateVASnippetFromSelection",
            "VAssistX.AddressResolver","VAssistX.PDBExplorer","VAssistX.VAMemoryView","File.Print"
        };
        internal static readonly HashSet<int> AllowedCa=new HashSet<int> {
            0x140,0x141,0x103,0x104,0x107,0x10a,0x10b,0x10c,0x116,0x117,0x11e,
            0x171,0x172,0x173,0x174,0x175,0x176,0x178,0x179,0x17a,
            0x180,0x181,0x182,0x183,0x184,0x185,0x186,0x187,0x188,0x189,0x18a,0x18b,0x18c,
            0x190,0x191,0x192,0x193,0x194,0x1c0,0x1c1
        };
    }
}
