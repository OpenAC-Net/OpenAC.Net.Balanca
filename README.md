# OpenAC.Net.Balanca

[![NuGet Version](https://img.shields.io/nuget/v/OpenAC.Net.Balanca.svg)](https://www.nuget.org/packages/OpenAC.Net.Balanca/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/OpenAC.Net.Balanca.svg)](https://www.nuget.org/packages/OpenAC.Net.Balanca/)
[![Discord](https://img.shields.io/badge/Chat%20on-Discord-purple.svg)](https://discord.com/invite/brdmJ7Yv6w)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![Target Frameworks](https://img.shields.io/badge/.NET%20Framework-4.6.2%20%7C%204.8-blue.svg)](https://dotnet.microsoft.com/)
[![Target Frameworks](https://img.shields.io/badge/.NET%20Core%20%2F%20Standard-netstandard2.0%20%7C%20net8.0%20%7C%20net9.0%20%7C%20net10.0-blue.svg)](https://dotnet.microsoft.com/)

Biblioteca .NET multiplataforma para **comunicação e leitura de balanças**, com suporte a diferentes fabricantes e protocolos de pesagem.

Desenvolvida pelo time **OpenAC .Net**, a biblioteca utiliza o `OpenAC.Net.Devices` para comunicação com os equipamentos e tem os protocolos do **ACBrBAL** como referência.

---

## 🚀 Funcionalidades

- **Leitura de Peso**: solicitação de pesagem e interpretação dos retornos do equipamento.
- **Múltiplos Protocolos**: 38 modelos específicos e um protocolo genérico configurável.
- **Comunicação Serial e TCP**: configuração pelo `OpenAC.Net.Devices`, com acesso fortemente tipado por `OpenBal<TConfig>`.
- **Monitoramento Contínuo**: acompanhamento das transmissões espontâneas da balança pelo evento `AoLerPeso`.
- **Estados de Pesagem**: identificação de instabilidade, peso negativo, sobrecarga e outros estados específicos de cada modelo.
- **Tratamento dos Retornos**: leitura de quadros fragmentados, timeout configurável e conversão decimal independente da cultura.
- **Demo Multiplataforma**: interface Avalonia com configurações, simulador sem hardware e histórico das respostas.
- **Testes Automatizados**: TUnit para protocolos, comunicação e ciclo de conexão, além de testes headless da interface.

---

## 🏛️ Protocolos Suportados

A biblioteca abstrai os formatos de solicitação e resposta dos seguintes modelos:

| Fabricante / família | Protocolos |
| --- | --- |
| **Toledo** | Toledo, Toledo2090, Toledo2090N, Toledo2180, ToledoBCS21, Toledo9091_8530_8540, ToledoTi420 |
| **Filizola** | Filizola |
| **Urano** | Urano, UranoPOP, UranoUDC |
| **Weightech** | WeightechWT1000, WeightechWT3000_ABS, WeightechWT3000IR_ABS, WeightechWT27R_ETH |
| **Libratek** | Libratek, LibratekWT3000IR |
| **Digitron** | Digitron, Digitron_UL |
| **Outros modelos** | AFTS, Alfa, Capital, LenkeLK2500, Lider, LucasTec, Magellan, Magna, MarelCG62XL, Marte, Micheletti, Muller, Precision, Ramuza, Rinnert, Saturno, Siciliano, Trentin, WeighTRUTest |
| **Genérica** | Extração do campo de peso por posição inicial e tamanho |

Consulte a [matriz de protocolos](src/docs/protocolos.md) para comandos, formatos, estados e particularidades de cada modelo. O protocolo e os parâmetros de comunicação devem corresponder ao modo configurado no equipamento.

---

## 📦 Instalação

Adicione o pacote via NuGet Package Manager ou CLI:

### .NET CLI:

```bash
dotnet add package OpenAC.Net.Balanca
```

### Package Manager Console:

```powershell
Install-Package OpenAC.Net.Balanca
```

### PackageReference:

```xml
<PackageReference Include="OpenAC.Net.Balanca" Version="1.6.0.1" />
```

---

## 🛠️ Como Usar

### 1. Inicializando e Configurando o Componente

```csharp
using System;
using System.IO.Ports;
using OpenAC.Net.Balanca;
using OpenAC.Net.Devices;

using var balanca = new OpenBal<SerialConfig>(new SerialConfig
{
    Porta = "COM1", // No Linux, por exemplo: /dev/ttyUSB0
    Baud = 9600,
    DataBits = 8,
    Parity = Parity.None,
    StopBits = StopBits.One,
    Handshake = Handshake.None,
    TimeOut = 3000 // Milissegundos
});

balanca.Protocolo = ProtocoloBalanca.Toledo;
balanca.DelayMonitoramento = 200;
```

A biblioteca suporta **.NET Framework 4.6.2 e 4.8**, **.NET Standard 2.0** e **.NET 8, 9 e 10**. Os exemplos usam sintaxe moderna de C#.

---

### 2. Lendo o Peso e Tratando os Retornos

```csharp
balanca.AoLerPeso += (_, leitura) =>
{
    if (leitura.Excecao != null)
    {
        Console.WriteLine($"Falha de leitura: {leitura.Excecao.Message}");
        return;
    }

    Console.WriteLine($"Peso/estado: {leitura.Peso}");
    Console.WriteLine($"Resposta bruta: {leitura.Leitura}");
};

balanca.Conectar();
var peso = balanca.LerPeso();
balanca.Desconectar();
```

Falhas de comunicação são informadas em `BalancaEventArgs.Excecao`, com `Peso` nulo; `LerPeso()` retorna `-9`. Chamadas sem conexão ativa lançam uma exceção.

Os valores negativos também podem representar estados específicos do protocolo:

| Valor | Significado |
| --- | --- |
| `-1` | Peso instável |
| `-2` | Peso negativo / subcarga |
| `-9` | Falha de comunicação / timeout |
| `-10` | Sobrecarga |
| `-11` | Captura inicial de zero |
| `-12` | Erro de calibração |

Alguns modelos possuem convenções próprias: o Toledo2090, por exemplo, pode retornar o peso com sinal negativo. Consulte a matriz antes de tratar o retorno como peso em quilogramas.

---

### 3. Monitorando Transmissões Contínuas

```csharp
balanca.Protocolo = ProtocoloBalanca.Lider;
balanca.DelayMonitoramento = 200;
balanca.IsMonitorar = true;
balanca.Conectar();

// O evento AoLerPeso recebe as leituras enquanto a conexão estiver ativa.
// Para interromper o monitoramento:
balanca.IsMonitorar = false;
balanca.Desconectar();
```

O monitor acompanha equipamentos que transmitem espontaneamente. Para modelos que dependem de solicitação, utilize `LerPeso()`. No monitoramento, o evento é disparado em uma thread de trabalho; em aplicações gráficas, encaminhe a atualização para a thread da interface.

---

### 4. Configurando uma Conexão TCP

```csharp
using var balancaTcp = new OpenBal<TCPConfig>(new TCPConfig("192.168.0.100", 9000)
{
    TimeOut = 3000
});

balancaTcp.Protocolo = ProtocoloBalanca.WeightechWT27R_ETH;
balancaTcp.Conectar();
var pesoTcp = balancaTcp.LerPeso();
balancaTcp.Desconectar();
```

O endereço, a porta e o protocolo dependem da configuração do equipamento.

---

### 5. Interpretando Respostas sem Equipamento

```csharp
using var simulador = new OpenBal<SerialConfig>
{
    Protocolo = ProtocoloBalanca.Toledo
};

// STX + peso + ETX: retorna 1,234 kg.
var pesoSimulado = simulador.InterpretarRespostaPeso("\u000201234\u0003");

// Protocolo genérico: posição inicial baseada em 1 e quantidade de caracteres.
simulador.Protocolo = ProtocoloBalanca.Generica;
simulador.PosicaoInicial = 3;
simulador.TamanhoResposta = 6;
var pesoGenerico = simulador.InterpretarRespostaPeso("PB012345kg"); // 12,345 kg
```

`InterpretarRespostaPeso()` não abre o dispositivo. A extração por posição e tamanho também é utilizada pelo protocolo Marte.

---

## 🖥️ Aplicação de Demonstração

O demo utiliza **Avalonia e .NET 8**, seguindo o padrão visual do NFSe Nacional, e oferece:

- Configuração dos protocolos e das conexões serial/TCP.
- Leitura manual e monitoramento das transmissões contínuas.
- Simulador de respostas sem equipamento conectado.
- Histórico de peso, estados e retornos brutos.
- Persistência das configurações no diretório de dados do usuário.

Execute a partir da raiz do repositório:

```bash
cd src
dotnet run --project OpenAC.Net.Balanca.Demo
```

Consulte o [README do demo](src/OpenAC.Net.Balanca.Demo/README.md) para mais detalhes.

---

## 🧪 Compilação e Testes

Para compilar a solução completa, utilize o **.NET SDK 10**, com os SDKs/runtimes correspondentes aos alvos executados.

```bash
cd src
dotnet restore OpenAC.Net.Balanca.sln
dotnet build OpenAC.Net.Balanca.sln -c Release
dotnet test --solution OpenAC.Net.Balanca.sln -c Release --no-build
```

Os testes utilizam **TUnit** e **Microsoft.Testing.Platform**:

- **OpenAC.Net.Balanca.Test**: interpretação de todos os protocolos em diferentes culturas, comandos, estados, fragmentação, estabilização, timeout, eventos e reconexão.
- **OpenAC.Net.Balanca.Demo.Test**: validação headless da janela Avalonia, seleção de transporte e simulação de retornos.

Os testes não acessam balanças físicas. A comunicação deve ser homologada com o equipamento e seu modo de transmissão.

---

## 🤝 Como Contribuir

Contribuições são muito bem-vindas! Para relatar um problema, sugerir melhorias ou enviar uma contribuição:

1. Faça um Fork do repositório.
2. Crie uma Branch para a sua feature (`git checkout -b feature/novo-protocolo`).
3. Faça o commit das suas alterações (`git commit -m 'Adiciona suporte a novo protocolo'`).
4. Envie para o GitHub (`git push origin feature/novo-protocolo`).
5. Abra um Pull Request.

Ao contribuir com um protocolo, inclua exemplos dos comandos e retornos e testes que cubram peso e estados do modelo.

---

## 💖 Ajude o Projeto OpenAC .Net

Se o **OpenAC.Net.Balanca** é útil para o seu negócio ou projeto open source e você deseja apoiar o desenvolvimento contínuo, considere fazer uma contribuição:

- Participe da nossa comunidade no [Discord](https://discord.com/invite/brdmJ7Yv6w).
- Contribua com código, testes e documentação no [GitHub](https://github.com/OpenAC-Net/OpenAC.Net.Balanca).
- Apoie o projeto pelo [Padrim](https://www.padrim.com.br/openac-net) ou [Apoia.se](https://apoia.se/openac-net).

---

## 🙏 Agradecimentos

Agradecimento especial aos projetos:

- [Gerene.Balanca](https://github.com/marcosgerene/Gerene.Balanca), pela doação do componente original.
- [brainiac](https://github.com/thiago132000/brainiac), como referência para comunicação serial em C#.
- [ACBr](https://www.projetoacbr.com.br/), como referência dos protocolos de balanças.

---

## 📄 Licença

Este projeto é distribuído sob a licença **MIT**. Consulte o arquivo [LICENSE](LICENSE) para mais detalhes.
