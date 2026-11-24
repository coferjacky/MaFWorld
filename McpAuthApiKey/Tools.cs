using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;

namespace McpAuthApiKey
{
    [McpServerToolType]
    public class Tools
    {
        [McpServerTool(Name ="get_the_secret_word",ReadOnly =true)]
        [Description("Get the Top Secret Word")]
        public string GetTheSecretWord()
        {
            return "BananaCake";
        }
    }
}
