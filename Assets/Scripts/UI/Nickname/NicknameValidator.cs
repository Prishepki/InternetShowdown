using UnityEngine;
using System;

namespace TMPro
{
    /// <summary>
    /// EXample of a Custom Character Input Validator to only allow digits from 0 to 9.
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "Nickname Validator.asset", menuName = "TextMeshPro/Input Validators", order = 100)]
    public class NicknameValidator : TMP_InputValidator
    {
        // Custom text input validation function
        public override char Validate(ref string text, ref int pos, char ch)
        {
            if (pos < 20)
            {
                if (ch >= '0' && ch <= '9')
                {
                    text = text.Insert(pos, ch.ToString());
                    pos += 1;
                    return ch;
                }
    
                if (ch >= 'a' && ch <= 'z')
                {
                    text = text.Insert(pos, ch.ToString());
                    pos += 1;
                    return ch;
                }
    
                if (ch >= 'а' && ch <= 'я')
                {
                    text = text.Insert(pos, ch.ToString());
                    pos += 1;
                    return ch;
                }

                if (ch >= ' ')
                {
                    text = text.Insert(pos, ch.ToString());
                    pos += 1;
                    return ch;
                }

                if (ch >= '.')
                {
                    text = text.Insert(pos, ch.ToString());
                    pos += 1;
                    return ch;
                }
            }

            return (char)0;
        }
    }
}
 