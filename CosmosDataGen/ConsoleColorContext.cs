﻿namespace CosmosDataGen
{
    internal class ConsoleColorContext : IDisposable
    {
        ConsoleColor beforeContextColor;

        public ConsoleColorContext(ConsoleColor color)
        {
            this.beforeContextColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
        }

        public void Dispose()
        {
            Console.ForegroundColor = this.beforeContextColor;
        }
    }
}
