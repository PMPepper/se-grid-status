using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD
{
    enum StringMatchPartType
    {
        SingleCharacterWildcard,
        MultiCharacterWildcard,
        Literal,
    }
    struct StringMatchPart
    {
        public StringMatchPartType Type;
        public string String;//only for literals

        public StringMatchPart(StringMatchPartType type, string str = null)
        {
            Type = type;
            String = str;
        }

        public override string ToString()
        {
            return $"<StringMatchPart Type=\"{Type}\" String=\"{String}\" />";
        }
    }
    //? = Single character wildcard - will match any one character
    //* = Multi character wildcard, will match any characters until the next filter character is matched (can be zero) e.g. the Filter "*World" would match "World", or "Hello World", but not "WWorld"
    //\ = literal modifier - the next character in the filter will be treated as it's literal value, so if you want to test for a ? character, use \?
    //any other character = matches that literal character
    //any wildcard following a multi character wildcard will be ignored
    public class StringMatcher
    {
        private string _Value;
        private List<StringMatchPart> _Parts = new List<StringMatchPart>();
        public string Value { get { return _Value; } set { 
                if(value == _Value)
                {
                    return;
                }

                _Parts.Clear();

                if(!string.IsNullOrEmpty(value))
                {
                    bool literalMode = false;
                    StringMatchPart currentLiteralPart = new StringMatchPart(StringMatchPartType.Literal, "");

                    foreach(char chr in value)
                    {
                        if(!literalMode)
                        {
                            if (chr == '\\')
                            {
                                literalMode = true;

                                continue;//enter literal mode and move on to next character
                            }
                            if (chr == '*' || chr == '?')
                            {
                                //If there is a current literal part 'collector', add to the parts list
                                if(!string.IsNullOrEmpty(currentLiteralPart.String))
                                {
                                    _Parts.Add(currentLiteralPart);
                                    currentLiteralPart = new StringMatchPart(StringMatchPartType.Literal, "");
                                }

                                if(_Parts.Count == 0 || _Parts.Last().Type != StringMatchPartType.MultiCharacterWildcard)
                                {
                                    //add wildcard part, and move on to next character - but never follow any wildcard after a multicharacter wildcard
                                    _Parts.Add(new StringMatchPart(chr == '*' ? StringMatchPartType.MultiCharacterWildcard : StringMatchPartType.SingleCharacterWildcard));
                                }
                                
                                continue;
                            }
                        }

                        currentLiteralPart.String += chr;
                        literalMode = false;
                    }

                    //finally, add any remaining literal part
                    if (!string.IsNullOrEmpty(currentLiteralPart.String))
                    {
                        _Parts.Add(currentLiteralPart);
                    }
                }

                _Value = value;
        } }

        public StringMatcher() { }

        public StringMatcher(string match)
        {
            Value = match;
        }

        public bool Test(string str)
        {
            //MyAPIGateway.Utilities.ShowMessage("[GSA]: ", $"find shield block");
            if (str == null)//null value always returns false
            {
                return false;
            }

            if(_Parts.Count == 0)
            {
                return true;
            }

            for(int i = 0; i < _Parts.Count; i++)
            {
                var part = _Parts[i];

                switch(part.Type)
                {
                    case StringMatchPartType.SingleCharacterWildcard:
                        if(str.Length == 0)
                        {
                            return false;
                        }
                        str = str.Substring(1);
                        break;
                    case StringMatchPartType.MultiCharacterWildcard:
                        if(i+1 >= _Parts.Count)
                        {
                            return true;//if this is the last filter part, then the filter has passed
                        }
                        //next part must be a literal (because wildcards can't follow one after the other, and there must be another part, or we would have already passed)
                        //so, skip until the next literal (if present)
                        var nextPart = _Parts[i + 1];
                        int index = str.IndexOf(nextPart.String);

                        if(index > 0)
                        {
                            str = str.Substring(index);
                        }
                        break;
                    case StringMatchPartType.Literal:
                        if(str.StartsWith(part.String))
                        {
                            str = str.Substring(part.String.Length);
                        } else
                        {
                            return false;
                        }
                        break;
                }
            }

            return str == "";
        }

    }
    
    static class TextUtils
    {
        public static string TextBar(float value, int width)
        {
            var sb = new StringBuilder();

            TextBar(sb, value, width);

            return sb.ToString();
        }

        public static void TextBar(StringBuilder sb, float value, int width)
        {
            if(value < 0)
            {
                value = 0;
            }

            if(value > 1)
            {
                value = 1;
            }

            int filledChars = (int)Math.Ceiling((float)width * value);

            sb.Append('{');

            for (int i = 0; i < width; i++)
            {
                //:/. works space wise, but looks weird
                if(i >= filledChars)
                {
                    sb.Append("..");
                } else
                {
                    sb.Append("[]");
                }
            }

            sb.Append('}');
        }
    }
}
