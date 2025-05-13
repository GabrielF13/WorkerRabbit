
# WorkerRabbit

O **WorkerRabbit** é uma aplicação desenvolvida em C# com o objetivo de implementar um worker utilizando RabbitMQ para processamento assíncrono de mensagens.
Este projeto visa demonstrar como configurar e executar um worker que consome mensagens de uma fila RabbitMQ, processando-as de forma eficiente e escalável.

## 🔧 Tecnologias Utilizadas

- .NET (C#)
- RabbitMQ
- Arquitetura de Worker

## 📁 Estrutura do Projeto

- `WorkerRabbit.sln`: Solução principal do projeto.
- `WorkerRabbit/`: Contém a implementação do worker que consome mensagens da fila RabbitMQ.

## 🚀 Como Executar o Projeto

1. Clone o repositório:
   ```bash
   git clone https://github.com/GabrielF13/WorkerRabbit.git
   cd WorkerRabbit
   ```

2. Restaure as dependências:
   ```bash
   dotnet restore
   ```

3. Configure o RabbitMQ:
   - Certifique-se de que o RabbitMQ está instalado e em execução.
   - Atualize as configurações de conexão no arquivo de configuração do projeto, se necessário.

4. Execute o worker:
   ```bash
   dotnet run --project WorkerRabbit
   ```

## 📌 Funcionalidades

- Conexão com o servidor RabbitMQ.
- Consumo de mensagens de uma fila específica.
- Processamento assíncrono das mensagens recebidas.
- Tratamento de exceções e reconexão automática em caso de falhas.

## 📄 Licença

Este projeto está licenciado sob a [MIT License](LICENSE).
