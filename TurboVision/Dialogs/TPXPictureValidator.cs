using TurboVision.Core;

namespace TurboVision.Dialogs;

/// <summary>
/// Result codes for picture validation.
/// </summary>
public enum TPicResult
{
    prComplete,
    prIncomplete,
    prEmpty,
    prError,
    prSyntax,
    prAmbiguous,
    prIncompNoFill
}

/// <summary>
/// Picture-based input validator.
///
/// Picture format special characters:
/// - #  : Digit (0-9)
/// - ?  : Any letter (a-z, A-Z)
/// - &amp;  : Any letter, converted to uppercase
/// - !  : Any character, converted to uppercase
/// - @  : Any character
/// - ;  : Escape next character (literal)
/// - *n : Repeat next character/group n times (0 = unlimited)
/// - {} : Required group
/// - [] : Optional group
/// - ,  : Alternative separator within groups
///
/// Examples:
/// - "###-##-####" : US Social Security Number
/// - "(###) ###-####" : US Phone Number
/// - "&amp;&amp;&amp;-####" : Mixed format
/// - "*3#-*2#" : Repeat patterns
/// </summary>
public class TPXPictureValidator : TValidator
{
    private static readonly string ErrorMsg = "Error in picture format.\n {0}";

    protected string? Pic { get; set; }
    private int _index;
    private int _jndex;

    public TPXPictureValidator(string pic, bool autoFill) : base()
    {
        Pic = pic;
        if (autoFill)
        {
            Options |= ValidatorOptions.voFill;
        }

        // Validate the picture syntax
        string s = "";
        if (Picture(ref s, false) != TPicResult.prEmpty)
        {
            Status = ValidatorStatus.vsSyntax;
        }
    }

    public override void Error()
    {
        MsgBox.MessageBox(
            MessageBoxFlags.mfError | MessageBoxFlags.mfOKButton,
            ErrorMsg,
            Pic ?? "");
    }

    public override bool IsValidInput(ref string s, bool suppressFill)
    {
        bool doFill = ((Options & ValidatorOptions.voFill) != 0) && !suppressFill;
        return Pic == null || Picture(ref s, doFill) != TPicResult.prError;
    }

    public override bool IsValid(string s)
    {
        string str = s;
        return Pic == null || Picture(ref str, false) == TPicResult.prComplete;
    }

    /// <summary>
    /// Processes input against the picture format.
    /// </summary>
    public virtual TPicResult Picture(ref string input, bool autoFill)
    {
        if (!SyntaxCheck())
        {
            return TPicResult.prSyntax;
        }

        if (string.IsNullOrEmpty(input))
        {
            return TPicResult.prEmpty;
        }

        _jndex = 0;
        _index = 0;

        char[] inputChars = input.ToCharArray();
        var result = Process(inputChars, Pic!.Length);

        // If we didn't consume all input, it's an error
        if (result != TPicResult.prError && _jndex < inputChars.Length)
        {
            result = TPicResult.prError;
        }

        // Auto-fill fixed characters if incomplete
        if (result == TPicResult.prIncomplete && autoFill)
        {
            bool reprocess = false;
            var sb = new System.Text.StringBuilder(new string(inputChars, 0, _jndex));

            while (_index < Pic.Length && !IsSpecial(Pic[_index], "#?&!@*{}[],"))
            {
                if (Pic[_index] == ';')
                {
                    _index++;
                }
                sb.Append(Pic[_index]);
                _index++;
                reprocess = true;
            }

            if (reprocess)
            {
                input = sb.ToString();
                _jndex = 0;
                _index = 0;
                inputChars = input.ToCharArray();
                result = Process(inputChars, Pic.Length);
            }
        }

        // Update input with any modifications (matching upstream behavior - no truncation)
        input = new string(inputChars);

        if (result == TPicResult.prAmbiguous)
        {
            return TPicResult.prComplete;
        }
        else if (result == TPicResult.prIncompNoFill)
        {
            return TPicResult.prIncomplete;
        }
        return result;
    }

    private bool SyntaxCheck()
    {
        if (string.IsNullOrEmpty(Pic))
        {
            return false;
        }

        if (Pic[Pic.Length - 1] == ';')
        {
            return false;
        }

        int brkLevel = 0;
        int brcLevel = 0;

        for (int i = 0; i < Pic.Length; i++)
        {
            switch (Pic[i])
            {
                case '[': brkLevel++; break;
                case ']': brkLevel--; break;
                case '{': brcLevel++; break;
                case '}': brcLevel--; break;
                case ';': i++; break;
            }
        }

        return brkLevel == 0 && brcLevel == 0;
    }

    private void Consume(char ch, char[] input)
    {
        if (_jndex < input.Length)
        {
            input[_jndex] = ch;
        }
        _index++;
        _jndex++;
    }

    private void ToGroupEnd(ref int i, int termCh)
    {
        int brkLevel = 0;
        int brcLevel = 0;

        do
        {
            if (i == termCh)
            {
                return;
            }

            switch (Pic![i])
            {
                case '[': brkLevel++; break;
                case ']': brkLevel--; break;
                case '{': brcLevel++; break;
                case '}': brcLevel--; break;
                case ';': i++; break;
            }
            i++;
        } while (brkLevel != 0 || brcLevel != 0);
    }

    private bool SkipToComma(int termCh)
    {
        do
        {
            ToGroupEnd(ref _index, termCh);
        } while (_index != termCh && Pic![_index] != ',');

        if (Pic![_index] == ',')
        {
            _index++;
        }
        return _index < termCh;
    }

    private int CalcTerm(int termCh)
    {
        int k = _index;
        ToGroupEnd(ref k, termCh);
        return k;
    }

    private TPicResult Iteration(char[] input, int inTerm)
    {
        int itr = 0;
        TPicResult rslt = TPicResult.prError;

        _index++; // Skip '*'

        // Retrieve repeat count
        while (_index < Pic!.Length && IsNumber(Pic[_index]))
        {
            itr = itr * 10 + (Pic[_index] - '0');
            _index++;
        }

        int k = _index;
        int termCh = CalcTerm(inTerm);

        if (itr != 0)
        {
            // Exact repetition
            for (int l = 1; l <= itr; l++)
            {
                _index = k;
                rslt = Process(input, termCh);
                if (!IsComplete(rslt))
                {
                    if (rslt == TPicResult.prEmpty)
                    {
                        rslt = TPicResult.prIncomplete;
                    }
                    return rslt;
                }
            }
        }
        else
        {
            // Unlimited repetition
            do
            {
                _index = k;
                rslt = Process(input, termCh);
            } while (rslt == TPicResult.prComplete);

            if (rslt == TPicResult.prEmpty || rslt == TPicResult.prError)
            {
                _index++;
                rslt = TPicResult.prAmbiguous;
            }
        }

        _index = termCh;
        return rslt;
    }

    private TPicResult Group(char[] input, int inTerm)
    {
        int termCh = CalcTerm(inTerm);
        _index++;
        var rslt = Process(input, termCh - 1);

        if (!IsIncomplete(rslt))
        {
            _index = termCh;
        }

        return rslt;
    }

    private TPicResult CheckComplete(TPicResult rslt, int termCh)
    {
        if (IsIncomplete(rslt))
        {
            int j = _index;
            bool status = true;

            while (status && j < Pic!.Length)
            {
                switch (Pic[j])
                {
                    case '[':
                        ToGroupEnd(ref j, termCh);
                        break;
                    case '*':
                        if (j + 1 < Pic.Length && !IsNumber(Pic[j + 1]))
                        {
                            j++;
                        }
                        ToGroupEnd(ref j, termCh);
                        break;
                    default:
                        status = false;
                        break;
                }
            }

            if (j == termCh)
            {
                rslt = TPicResult.prAmbiguous;
            }
        }

        return rslt;
    }

    private TPicResult Scan(char[] input, int termCh)
    {
        TPicResult rScan = TPicResult.prError;
        TPicResult rslt = TPicResult.prEmpty;

        while (_index != termCh && Pic![_index] != ',')
        {
            if (_jndex >= input.Length)
            {
                return CheckComplete(rslt, termCh);
            }

            char ch = input[_jndex];

            switch (Pic[_index])
            {
                case '#':
                    if (!IsNumber(ch))
                    {
                        return TPicResult.prError;
                    }
                    Consume(ch, input);
                    break;

                case '?':
                    if (!IsLetter(ch))
                    {
                        return TPicResult.prError;
                    }
                    Consume(ch, input);
                    break;

                case '&':
                    if (!IsLetter(ch))
                    {
                        return TPicResult.prError;
                    }
                    Consume(char.ToUpperInvariant(ch), input);
                    break;

                case '!':
                    Consume(char.ToUpperInvariant(ch), input);
                    break;

                case '@':
                    Consume(ch, input);
                    break;

                case '*':
                    rslt = Iteration(input, termCh);
                    if (!IsComplete(rslt))
                    {
                        return rslt;
                    }
                    if (rslt == TPicResult.prError)
                    {
                        rslt = TPicResult.prAmbiguous;
                    }
                    break;

                case '{':
                    rslt = Group(input, termCh);
                    if (!IsComplete(rslt))
                    {
                        return rslt;
                    }
                    break;

                case '[':
                    rslt = Group(input, termCh);
                    if (IsIncomplete(rslt))
                    {
                        return rslt;
                    }
                    if (rslt == TPicResult.prError)
                    {
                        rslt = TPicResult.prAmbiguous;
                    }
                    break;

                default:
                    if (Pic[_index] == ';')
                    {
                        _index++;
                    }

                    if (char.ToUpperInvariant(Pic[_index]) != char.ToUpperInvariant(ch))
                    {
                        if (ch == ' ')
                        {
                            ch = Pic[_index];
                        }
                        else
                        {
                            return rScan;
                        }
                    }

                    Consume(Pic[_index], input);
                    break;
            }

            if (rslt == TPicResult.prAmbiguous)
            {
                rslt = TPicResult.prIncompNoFill;
            }
            else
            {
                rslt = TPicResult.prIncomplete;
            }
        }

        if (rslt == TPicResult.prIncompNoFill)
        {
            return TPicResult.prAmbiguous;
        }
        return TPicResult.prComplete;
    }

    private TPicResult Process(char[] input, int termCh)
    {
        TPicResult rslt;
        bool incomp = false;
        int incompJ = 0, incompI = 0;
        int oldI = _index;
        int oldJ = _jndex;

        do
        {
            rslt = Scan(input, termCh);

            // Only accept completes if they make it farther in the input
            if (rslt == TPicResult.prComplete && incomp && _jndex < incompJ)
            {
                rslt = TPicResult.prIncomplete;
                _jndex = incompJ;
            }

            if (rslt == TPicResult.prError || rslt == TPicResult.prIncomplete)
            {
                TPicResult rProcess = rslt;

                if (!incomp && rslt == TPicResult.prIncomplete)
                {
                    incomp = true;
                    incompI = _index;
                    incompJ = _jndex;
                }

                _index = oldI;
                _jndex = oldJ;

                if (!SkipToComma(termCh))
                {
                    if (incomp)
                    {
                        rProcess = TPicResult.prIncomplete;
                        _index = incompI;
                        _jndex = incompJ;
                    }
                    return rProcess;
                }
                oldI = _index;
            }
        } while (rslt == TPicResult.prError || rslt == TPicResult.prIncomplete);

        if (rslt == TPicResult.prComplete && incomp)
        {
            return TPicResult.prAmbiguous;
        }
        return rslt;
    }

    private static bool IsNumber(char ch)
    {
        return ch >= '0' && ch <= '9';
    }

    private static bool IsLetter(char ch)
    {
        return (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z');
    }

    private static bool IsSpecial(char ch, string special)
    {
        return special.Contains(ch);
    }

    private static bool IsComplete(TPicResult result)
    {
        return result == TPicResult.prComplete || result == TPicResult.prAmbiguous;
    }

    private static bool IsIncomplete(TPicResult result)
    {
        return result == TPicResult.prIncomplete || result == TPicResult.prIncompNoFill;
    }
}
