using Microsoft.Extensions.AI;

namespace MaFUtilities.Extensions;
//
//extension 块（扩展方法组），作用是给 UsageDetails? 类型追加一个打印 token 用量的方法。专门用来显示 AI 调用的"计费明细"。
//
public static class UsageDetailsExtensions
{
    extension(UsageDetails? usageDetails)
    {
        public void OutputAsInformation()
        {
            if (usageDetails == null)
            {
                return;
            }

            Utils.Gray($"- Input Tokens: {usageDetails.InputTokenCount}");
            string output = $"- Output Tokens: {usageDetails.OutputTokenCount} " + $"({usageDetails.ReasoningTokenCount ?? 0} was used for reasoning)";
            Utils.Gray(output);
        }
    }
}