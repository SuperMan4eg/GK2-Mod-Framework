using System;
using System.Reflection;
using System.Reflection.Emit;

namespace GK2.Framework
{
    public static class Gk2CompatibilityInspector
    {
        private static readonly OpCode[] SingleByteOpCodes = new OpCode[256];
        private static readonly OpCode[] MultiByteOpCodes = new OpCode[256];

        static Gk2CompatibilityInspector()
        {
            foreach (FieldInfo field in typeof(OpCodes).GetFields(
                BindingFlags.Public | BindingFlags.Static))
            {
                if (!(field.GetValue(null) is OpCode opCode))
                    continue;

                ushort value = unchecked((ushort)opCode.Value);
                if (value < 0x100)
                    SingleByteOpCodes[value] = opCode;
                else if ((value & 0xff00) == 0xfe00)
                    MultiByteOpCodes[value & 0xff] = opCode;
            }
        }

        public static bool Calls(MethodBase source, MethodBase target)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            MethodBody body = source.GetMethodBody();
            byte[] il = body?.GetILAsByteArray();
            if (il == null || il.Length == 0)
                return false;

            int position = 0;
            while (position < il.Length)
            {
                OpCode opCode = ReadOpCode(il, ref position);
                int operandOffset = position;

                if ((opCode == OpCodes.Call || opCode == OpCodes.Callvirt)
                    && operandOffset + 4 <= il.Length)
                {
                    int token = BitConverter.ToInt32(il, operandOffset);
                    MethodBase called = ResolveMethod(source, token);
                    if (SameMethod(called, target))
                        return true;
                }

                position += GetOperandSize(opCode.OperandType, il, operandOffset);
            }

            return false;
        }

        private static OpCode ReadOpCode(byte[] il, ref int position)
        {
            byte first = il[position++];

            if (first != 0xfe)
                return SingleByteOpCodes[first];

            if (position >= il.Length)
                throw new BadImageFormatException("Truncated two-byte IL opcode.");

            return MultiByteOpCodes[il[position++]];
        }

        private static int GetOperandSize(
            OperandType operandType,
            byte[] il,
            int operandOffset)
        {
            switch (operandType)
            {
                case OperandType.InlineNone:
                    return 0;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    return 1;
                case OperandType.InlineVar:
                    return 2;
                case OperandType.InlineI:
                case OperandType.InlineBrTarget:
                case OperandType.InlineField:
                case OperandType.InlineMethod:

                case OperandType.InlineSig:
                case OperandType.InlineString:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                case OperandType.ShortInlineR:
                    return 4;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    return 8;
                case OperandType.InlineSwitch:
                    if (operandOffset + 4 > il.Length)
                        throw new BadImageFormatException("Truncated IL switch operand.");
                    int count = BitConverter.ToInt32(il, operandOffset);
                    return 4 + checked(count * 4);
                default:
                    throw new NotSupportedException(
                        "Unsupported IL operand type: " + operandType);
            }
        }

        private static MethodBase ResolveMethod(MethodBase source, int token)
        {
            try
            {
                Type[] typeArguments = source.DeclaringType?.IsGenericType == true
                    ? source.DeclaringType.GetGenericArguments()
                    : null;
                Type[] methodArguments = source.IsGenericMethod

                    ? source.GetGenericArguments()
                    : null;
                return source.Module.ResolveMethod(
                    token,
                    typeArguments,
                    methodArguments);
            }
            catch
            {
                return null;
            }
        }

        private static bool SameMethod(MethodBase left, MethodBase right)
        {
            if (left == null || right == null)
                return false;

            return left.Module == right.Module
                && left.MetadataToken == right.MetadataToken;
        }
    }
}
