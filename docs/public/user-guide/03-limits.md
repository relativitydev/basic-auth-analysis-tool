# Limits of the tool (User Guide, section 3 of 8)

This section says what the tool does not do, how well it did on its test repository, and what a result does and does not mean. Read it before you rely on a report.

## Languages delivered so far

This release analyzes **C# only**. The other seven languages of the plan (VB.NET, JavaScript, TypeScript, Python, Go, PowerShell and Shell) are delivered one at a time; see [What's next](02-release-notes.md#whats-next).

**Files in languages that are not delivered yet are not analyzed. An empty result for them does not mean that no Basic Authentication was found.** The scan summary of the report counts those files so that you can see how many there are. The same holds inside C#: the tool reports what its catalog searches for, and a run with no findings means that none of the constructs the catalog covers was found in the C# files it could read. A run that finds nothing completes normally, with exit code 0, and its report says that it found zero findings.

## Folders the walk does not enter

The walk does not enter eight folder names, matched exactly and in lower case at any depth: `bin`, `obj`, `.git`, `.vs`, `node_modules`, `dist`, `build` and `packages`. Nothing in them is read, so hand-written code or scripts kept in a folder with one of those names are not assessed. The report names the excluded folders and counts them, so the gap is visible; they are not skipped items. No other folder name is excluded. See [What the scan reads and what it leaves out](01-using-the-tool.md#what-the-scan-reads-and-what-it-leaves-out).

## Files that are too large

The tool analyzes a file only if it is at most 2 MiB (2,097,152 bytes) and its longest line has at most 10,000 characters. A file above either limit is not analyzed at all, and the report counts it under **Files larger than this release analyzes** or **Files with a line longer than this release analyzes** and names it. Review such a file by hand. A minified or machine-written file is the usual cause.

## Patterns the catalog does not cover

The catalog declares what it does not cover, and the report prints the same list in its Coverage section. The list is in [Part A, Declared unsupported coverage](../migration/part-a-csharp.md#declared-unsupported-coverage). It includes code in Razor views and in other .NET languages, credentials in configuration files, credentials passed through dependency injection or configuration binding, a user name and password written into a URL, calls made by compiled libraries, code inside preprocessor conditional blocks, and files in older single-byte encodings. A construct on that list produces no finding; review those areas by hand, or search for them with the patterns in Part A.

## Accuracy thresholds and what was measured

The tool is measured against a benchmark test repository that holds planted Basic Authentication patterns and deliberate near-misses (constructs that resemble Basic Authentication and are not). The targets are these:

| Measure | Target |
| --- | --- |
| Recall on planted patterns the catalog claims to support | at least 90 % reported at High or Medium |
| False-positive rate on near-misses | at most 10 % reported at High or Medium |
| Recall floor for each language | at least 80 % |
| Recommendation coverage | 100 %: every High or Medium finding carries a recommendation for its flow |
| Flow-assignment accuracy | at least 90 % overall and at least 90 % for each expected flow |

These are NSerio's recommended starting thresholds. They have not been confirmed by Relativity.

The figures for C# in this release were measured on Windows and on Linux. The Linux run used a virtual machine on the same computer as the Windows run, so it is another operating system and not other hardware. **The measurement on macOS is pending.** Percentages are rounded to one decimal. Beside each rate the table gives the lower end of its 95 % Wilson confidence interval, because the populations are small.

**Recall and false positives are measured on the first set of planted items, and that measurement is not blind.** Those items were read while the catalog was being diagnosed, so the two figures are not independent evidence.

| Measure | Result | Lower bound (95 %) | Target |
| --- | --- | --- | --- |
| Recall on catalogued patterns, without the one manual-review class (`CS-011`) | 30 of 33 = 90.9 % | 76.4 % | at least 90 %: met, with no margin |
| Recall with that class included | 30 of 34 = 88.2 % | 73.4 % | reported for reference; not gated |
| False-positive rate on near-misses | 0 of 28 = 0.0 % | 0.0 % (the upper end of the interval is 12.1 %) | at most 10 %: met |

`CS-011` is the class that marks a helper that builds an authentication header. It is always routed to manual review, so it does not count toward the recall that is gated. The three catalogued patterns that were not found are recorded catalog gaps.

**Flow accuracy and recommendation coverage are measured on the newer sets of planted items**, including a second batch planted before the latest detection corrections. These 80 detected files were measured blind, and "blind" here means blind to the tool's signal expressions and to the analyzer source. It does **not** mean blind to the catalog. The flow figures therefore measure how consistent the tool is with its own catalog. They are not a measurement of accuracy on real code.

| Measure | Result | Lower bound (95 %) | Target |
| --- | --- | --- | --- |
| Flow accuracy, overall | 80 of 80 = 100.0 % | 95.4 % | at least 90 %: met |
| Flow accuracy, expected Authorization Code | 28 of 28 = 100.0 % | 87.9 % | at least 90 %: met |
| Flow accuracy, expected Client Credentials | 52 of 52 = 100.0 % | 93.1 % | at least 90 %: met |
| Recommendation coverage for the assigned flow | 80 of 80 = 100.0 % | 95.4 % | 100 %: met |

With 28 files expecting Authorization Code, the lower bound of that rate lies below 90 %. Read the 100 % as "no miss in a small population", not as a guarantee. C# is the only language in this release, and its recall of 90.9 % is above the 80 % floor.

For completeness, these figures are disclosed and do **not** gate: all planted items together, recall of 110 of 115 = 95.7 % (110 of 116 = 94.8 % with `CS-011`); detection of the newer sets, 80 of 82 = 97.6 %; and flow assignment on the first set, 30 of 30 = 100.0 %.

## Low confidence needs review

A finding at **Low** confidence is present in the text but unlikely to be live authenticating code: a comment, a documentation string, a test fixture, sample or example code, vendored code, or a generated file. Sample and example code means a folder named `sample`, `samples`, `example` or `examples`, or a file whose name begins with `Sample` or `Example`; vendored code means a folder named `vendor` or `third-party`. A sample kept under another name, such as a folder called `demo`, is not recognized and is reported at its ordinary level. [Part A](../migration/part-a-csharp.md#how-confidence-is-assigned) lists every signal. The tool gives a Low finding no flow and no recommendation and routes it to manual review. It is not evidence that the code does not authenticate. [Part C](../migration/part-c-csharp.md#judging-a-low-confidence-match) explains how to judge each cause.

## The flow is chosen from the finding's own file

The tool decides between Client Credentials and Authorization Code from signals in the same file as the finding, and only there. A login page, a login handler or a credential prompt in a different file is not seen, and neither is a credential that arrives through dependency injection or configuration binding. In real code the credential usually reaches the finding's file from another file, so **`Default` is the expected decision for most real integrations**: the finding gets the default flow, and its note asks you who supplies the credential at run time. That is not a failure of the tool; it is the reason the note exists. [Confirming the flow](../migration/part-c-csharp.md#confirming-the-flow) in Part C explains how to decide by hand. Pointing to evidence found elsewhere in the same repository is planned after the alpha.

## The findings limits

The tool limits how many findings it reports: **2,000 for one file and 50,000 for one scan**. These limits are a bound on pathological or generated input, such as a generated file with an enormous number of matches. An ordinary repository produces a few dozen findings, far below them, so you should never meet a limit in ordinary use. If you do see a notice that findings were counted and not listed, a generated or machine-written file is the likely cause. The report states how many findings each limit removed. The findings that are kept are the first ones in a fixed order (by path for a whole scan, by position for one file), the findings that are dropped are not named or located individually, and the counts stay correct. Find the file that produced the notice in the report, and review it by hand.

## A file that takes too long

The tool bounds the time it spends evaluating each of its search expressions on a file. If a file reaches the limit, the tool discards every finding for that file and counts the file in the report's skipped items, so the loss shows up as a number. Review that file by hand. The limit is provisional; see the [release notes](02-release-notes.md#known-issues).

## File names in decomposed Unicode form

The tool identifies a file by its path as the platform returns it, with the separators normalized. It does not convert file names to a single Unicode normalization form. Two file names that differ only in normalization form are therefore reported as **two files**. A name that reaches the tool in decomposed form, that is an accented letter stored as a letter plus a combining accent, may be positioned and identified differently from the same repository scanned on another platform. This applies on **Windows (NTFS)**, where such a name can exist, on **Linux**, whose file systems keep a name byte for byte and can therefore hold a decomposed name or two spellings of one name, and on **macOS**, which returns some names in decomposed form. Finding identifiers and the order of findings can then differ between platforms for those files. Detection within each file is not affected. The findings of a scan that reaches the limit of 50,000 findings are the exception: the ones kept are the first in path order (see [Limits on the number of findings](#the-findings-limits)), path order depends on the normalization form, and so the findings kept can differ between platforms.
