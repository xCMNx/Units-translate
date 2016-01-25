﻿namespace Core
{
    public interface IMapItemRange
    {
        /// <summary>
        /// Индекс начала области
        /// </summary>
        int Start { get; }

        /// <summary>
        /// Индекс конца области
        /// </summary>
        int End { get; }
    }
}