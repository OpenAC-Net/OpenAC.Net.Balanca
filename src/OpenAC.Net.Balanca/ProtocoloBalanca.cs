// ***********************************************************************
// Assembly         : OpenAC.Net.Balanca
// Author           : Marcos Gerene
// Created          : 11-09-2021
//
// Last Modified By : RFTD
// Last Modified On : 11-09-2021
// ***********************************************************************
// <copyright file="ProtocoloBalanca.cs" company="OpenAC .Net">
//		        		   The MIT License (MIT)
//	     		    Copyright (c) 2014-2022 Projeto OpenAC .Net
//
//	 Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//	 The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//	 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace OpenAC.Net.Balanca;

/// <summary>
/// Protocolos suportados para comunicação com balanças.
/// </summary>
public enum ProtocoloBalanca
{
    /// <summary>Protocolo Toledo.</summary>
    Toledo = 0,
    /// <summary>Protocolo Filizola.</summary>
    Filizola = 1,
    /// <summary>Protocolo Toledo2090.</summary>
    Toledo2090 = 2,
    /// <summary>Protocolo Toledo2180.</summary>
    Toledo2180 = 3,
    /// <summary>Protocolo Urano.</summary>
    Urano = 4,
    /// <summary>Protocolo LucasTec.</summary>
    LucasTec = 5,
    /// <summary>Protocolo Magna.</summary>
    Magna = 6,
    /// <summary>Protocolo Digitron.</summary>
    Digitron = 7,
    /// <summary>Protocolo Magellan.</summary>
    Magellan = 8,
    /// <summary>Protocolo UranoPOP.</summary>
    UranoPOP = 9,
    /// <summary>Protocolo Lider.</summary>
    Lider = 10,
    /// <summary>Protocolo Rinnert.</summary>
    Rinnert = 11,
    /// <summary>Protocolo Muller.</summary>
    Muller = 12,
    /// <summary>Protocolo Saturno.</summary>
    Saturno = 13,
    /// <summary>Protocolo AFTS.</summary>
    AFTS = 14,
    /// <summary>Protocolo Generica.</summary>
    Generica = 15,
    /// <summary>Protocolo Libratek.</summary>
    Libratek = 16,
    /// <summary>Protocolo Micheletti.</summary>
    Micheletti = 17,
    /// <summary>Protocolo Alfa.</summary>
    Alfa = 18,
    /// <summary>Protocolo Toledo9091 8530 8540.</summary>
    Toledo9091_8530_8540 = 19,
    /// <summary>Protocolo WeightechWT1000.</summary>
    WeightechWT1000 = 20,
    /// <summary>Protocolo MarelCG62XL.</summary>
    MarelCG62XL = 21,
    /// <summary>Protocolo WeightechWT3000 ABS.</summary>
    WeightechWT3000_ABS = 22,
    /// <summary>Protocolo Toledo2090N.</summary>
    Toledo2090N = 23,
    /// <summary>Protocolo ToledoBCS21.</summary>
    ToledoBCS21 = 24,
    /// <summary>Protocolo Precision.</summary>
    Precision = 25,
    /// <summary>Protocolo Digitron UL.</summary>
    Digitron_UL = 26,
    /// <summary>Protocolo LibratekWT3000IR.</summary>
    LibratekWT3000IR = 27,
    /// <summary>Protocolo ToledoTi420.</summary>
    ToledoTi420 = 28,
    /// <summary>Protocolo WeightechWT27R ETH.</summary>
    WeightechWT27R_ETH = 29,
    /// <summary>Protocolo Capital.</summary>
    Capital = 30,
    /// <summary>Protocolo Marte.</summary>
    Marte = 31,
    /// <summary>Protocolo LenkeLK2500.</summary>
    LenkeLK2500 = 32,
    /// <summary>Protocolo WeighTRUTest.</summary>
    WeighTRUTest = 33,
    /// <summary>Protocolo UranoUDC.</summary>
    UranoUDC = 34,
    /// <summary>Protocolo Siciliano.</summary>
    Siciliano = 35,
    /// <summary>Protocolo Trentin.</summary>
    Trentin = 36,
    /// <summary>Protocolo WeightechWT3000IR ABS.</summary>
    WeightechWT3000IR_ABS = 37,
    /// <summary>Protocolo Ramuza.</summary>
    Ramuza = 38
}
