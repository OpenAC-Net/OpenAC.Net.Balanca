# Protocolos de balanças

A revisão usa como referência as unidades `ACBrBAL*.pas` de
`/home/rfteno/Projetos/ACBr/Fontes/ACBrSerial/`, consultadas em 28/10/2026.
Os 38 modelos concretos registrados em `ACBrBAL.pas`, mais Genérica, estão
representados em `ProtocoloBalanca`. Os valores anteriores são preservados:
`Toledo = 0` e `Filizola = 1`. Os demais valores não correspondem ao enum do ACBr.

## Solicitação e interpretação

`ENQ` = byte 05; `EOT` = byte 04. Os comandos textuais são enviados sem quebra
adicional de linha. O padrão é limpar o buffer antes de solicitar um peso.
A exceção Toledo2090 envia ENQ sem limpar. Modelos contínuos não enviam comando
nem limpam os dados recebidos.

| Modelo | Solicitação | Campo / comportamento |
| --- | --- | --- |
| Toledo | ENQ | A, B, C/P05, P03 e Ethernet; status e formato self-checkout |
| Filizola | ENQ | STX até ETX/CR, com ou sem decimal; repete se instável |
| Toledo2090 | ENQ, sem limpar | Quadros de 16/17 caracteres, divisor 1000/100 e sinal/status |
| Toledo2090N | ENQ | Peso e tara, estados x/r/s/y |
| Toledo2180, ToledoTi420 | ENQ | Campo após acento grave; 5 amostras, as 3 últimas devem coincidir |
| Urano | ENQ | Campo PESO/N0 até kg; repete se instável |
| UranoUDC | EOT | Mesmo retorno Urano |
| UranoPOP | ENQ | USE-CB2 (PESO L) e USE-P2 (kg); espera 600 ms |
| LucasTec | ENQ | Peso bruto, tara e líquido; 4 amostras, as 3 últimas devem coincidir |
| Magna | P | Campo entre TAB/espaço e kg |
| Magellan | W | Formatos A/B/C |
| Digitron | ENQ | STX/ETX, divisor 100 para valor sem separador |
| Digitron_UL | ENQ | Prefixos D/E/F/@; divisor 100; quadro sem terminador após silêncio de 50 ms |
| Lider | Contínuo | Último SOH…E/STX estável; usa instável se não houver estável |
| Rinnert, Muller | ENQ | Campo após +, com separador explícito |
| Saturno | ENQ | Dígitos Latin-1 codificados e sufixos E/O |
| AFTS | ENQ | Campo de 7 caracteres na posição 2, invertido |
| Genérica | ENQ | `PosicaoInicial` e `TamanhoResposta`; divisor padrão 1000 |
| Marte | D05 | Campo configurável como Genérica |
| Libratek | Contínuo | ST/OL/US; peso a partir da posição 7 |
| Micheletti | ENQ | 7 caracteres a partir da posição 4 |
| Alfa | ENQ | 6 caracteres a partir da posição 5, divisor 100 |
| Toledo9091_8530_8540 | ENQ | Peso após palavras de estado, com ou sem espaço, sem divisor |
| WeightechWT1000 | ENQ | Flag de estabilidade e peso líquido na posição 19 |
| MarelCG62XL | ENQ | 9 caracteres a partir de STX + 14 |
| WeightechWT3000_ABS | ENQ | ST/US/OL, campo numérico e unidade |
| LibratekWT3000IR | ENQ | ST/US/OL e sinal |
| WeightechWT3000IR_ABS | ENQ | Fm1/Fm2/Fm9 de 19 caracteres, GS/NT/TR e sinal |
| ToledoBCS21 | ENQ | Prefixos S+/S-/D+/D- |
| Precision | Contínuo | Peso e estados G/F/A/@/H/T/M/L/I |
| WeightechWT27R_ETH | ENQ | Estabilidade, sinal, unidade kg/g, bruto e sobrecarga 999999 |
| Capital | ENQ | Campo após último E |
| LenkeLK2500 | Contínuo | Campo de 8 caracteres e estados MP/MN |
| WeighTRUTest | {RW} | [peso] / [Upeso] |
| Siciliano | ENQ | IP09 e IP10; bruto/tara e peso negativo |
| Trentin | ENQ | Condec e EL05 |
| Ramuza | ENQ | SOF5A/SOF06 e self-checkout |

## Estados

| Valor | Significado |
| --- | --- |
| -1 | Peso instável |
| -2 | Peso negativo / subcarga |
| -9 | Falha de comunicação / timeout |
| -10 | Sobrecarga |
| -11 | Captura inicial de zero |
| -12 | Erro de calibração |

A interpretação preserva a convenção do modelo: por exemplo, Toledo2090 devolve
um peso com sinal negativo, enquanto outros modelos usam -2 como estado.
Não trate todo valor negativo como um peso válido. `BalancaEventArgs.Excecao`
contém falhas de comunicação; `Peso` fica nulo nesse caso e `LerPeso()` retorna -9.

## Decisões da revisão

- A conversão aceita ponto e vírgula decimal, independentemente da cultura.
- A posição inicial do protocolo genérico começa em 1. O tamanho representa uma
  quantidade de caracteres, como o parâmetro de comprimento de `Copy` do Pascal.
- Os campos Toledo9091 são relativos ao STX, conforme o formato documentado no
  ACBr; a revisão corrige o deslocamento presente na implementação Pascal.
- Muller preserva o separador decimal quando há um campo iniciado por +,
  inclusive quando a extração do campo não inclui ETX.
- WT3000 trata ST/US/OL e aceita o campo compacto seguido por kg. A implementação
  de referência do WT3000_ABS apresenta verificações de tamanho/status que não
  correspondem ao próprio exemplo documentado.
- A codificação de comunicação é Latin-1 para preservar os bytes dos protocolos,
  incluindo os dígitos codificados da Saturno e palavras de estado binárias.
- Leituras fragmentadas são acumuladas até o terminador. Quadros incompletos
  remanescentes são preservados para a próxima leitura. O timeout usa
  `Device.TimeOut` em milissegundos; nunca se publica um peso antigo após falha.
- O monitor lê transmissões espontâneas. Para equipamentos que só respondem a
  solicitações, use `LerPeso()`. Operações no dispositivo são serializadas e os
  eventos são disparados fora do bloqueio de comunicação.

O escopo desta revisão é solicitação e leitura de peso. Os comandos opcionais
ACBr de envio de preço por kg, tara, zero e display não fazem parte desta API.
Checksums não são validados, assim como na interpretação de referência consultada.
Os testes são determinísticos e não acessam equipamento físico. A comunicação
real precisa ser verificada com cada modelo e seu modo de transmissão configurado.

## Validação

```sh
cd src
dotnet restore OpenAC.Net.Balanca.sln
dotnet build OpenAC.Net.Balanca.sln -c Release
dotnet test --solution OpenAC.Net.Balanca.sln -c Release --no-build
```

Os testes TUnit cobrem respostas de todos os modelos em pt-BR/en-US/de-DE,
comandos e limpeza de buffer, estados, variantes Toledo, fragmentação, amostras,
timeout, eventos, falha de conexão, reconexão e monitoramento. O projeto
`OpenAC.Net.Balanca.Demo.Test` valida a janela Avalonia em modo headless,
a seleção serial/TCP e a simulação de peso/instabilidade.
