# API em controllers, documentada pelo Swagger

A `YMS.Api` é uma Web API com controllers (`[ApiController]`), não uma Minimal API. A documentação de cada endpoint sai no Swagger UI (Swashbuckle, em `/swagger`). A primeira versão da spec #1 usava Minimal API, e o usuário pediu a troca: a API de integração precisa ser uma Web API com documentação completa pelo Swagger. Novos endpoints seguem o mesmo padrão: um controller por área, sobre o `ApiControllerBase`.

## Consequências

- O Swagger é gerado a partir dos comentários XML da `YMS.Api`, da `YMS.Application` e da `YMS.Domain` (`ApiSetup.cs`). Por isso o `<summary>` de um Command, de um campo ou de uma action faz parte do contrato publicado para o ERP. Quando uma regra da spec muda, o comentário muda junto.
- **Esses comentários são escritos em português**, porque o público do Swagger é o time do ERP do Owner. É a única exceção à regra "todo o código é em inglês" (`docs/design/modelagem-dominio.md`): nomes, rotas, campos do contrato e os comentários que não aparecem no Swagger continuam em inglês.
