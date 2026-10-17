# YMS referência: resumo do domínio do sistema de referência

> Leitura feita em 07/10/2026, somente leitura, sobre o backend (API principal, `jobs`, `schedulejob`) e o frontend.
> Credenciais, connection strings, URLs internas e dados de clientes foram deixados de fora de propósito.
> Onde está escrito "(inferido)", a conclusão veio da leitura de nomes e estruturas, não de um fluxo executado.

## 1. O que o sistema faz, em uma frase

O sistema de referência é um sistema de **agendamento de recebimento em CDs (centros de distribuição)**: fornecedores e transportadoras agendam entregas de **pedidos de compra** em **janelas de horário** de um CD, o sistema valida **capacidade** e **regras comerciais**, encaminha exceções para **aprovação**, controla a **nota fiscal (NFe)** vinculada, acompanha o **veículo no pátio** (portaria → em trânsito → recebido → checkout) e registra o **recebimento** das quantidades.

Hoje o "Y" de YMS (yard management) é a parte mais nova e mais fina do sistema: veículos, motoristas, empresas de transporte e um quadro kanban de status. O núcleo é o **dock scheduling** (agendamento de doca).

## 2. Glossário

| Termo no código | Termo de negócio | Observação |
|---|---|---|
| Owner | Cliente / tenant | Quase toda entidade tem `OwnerId`. Sistema multi-tenant por documento. |
| Warehouse | CD (centro de distribuição) | Tem janelas por dia da semana, capacidades, docas e prazos. |
| Order / OrderLine | Pedido de compra / item | Vem do ERP por integração. Tem `LeadTime` (data prevista). |
| Delivery / DeliveryLine | Entrega agendada (agendamento) | Rótulo `ENTR{n}`. Agrupa itens de 1+ pedidos, 1+ fornecedores. |
| DeliveryInterval / DeliveryWindow | Janela de recebimento | Faixa de horário (`TimeInterval`) com capacidade própria. |
| CapacityType | Unidade de capacidade | Unidades, SKUs ou Caixas (volumes). |
| Invoice / InvoiceLine | Nota fiscal (NFe) | Importada do XML (chave de acesso, CFOP, EAN, transportador). |
| Supplier | Fornecedor | CNPJ, linhas de fornecimento, fornecedor pai. |
| SupplierGroup | Grupo de fornecedores | Regras especiais por CD: dias, janela, reserva de capacidade, nº máx. de carros, antecedência. |
| AutoLockGroup | Grupo de bloqueio ou aprovação automática | Força a entrega a ficar Pendente (bloqueio) ou Agendada (auto‑aprovação). |
| Permission (na entrega) | Violação que precisa de aprovação | Cada regra quebrada vira uma "permissão" que só certos grupos de usuários podem aprovar. |
| UserGroup | Grupo de usuários | Lista de permissões que o grupo pode aprovar e quais ele recebe por notificação. |
| SpecialDate | Data especial / feriado | Nacional, estadual, municipal ou especial; altera capacidade em um período. |
| TransferForecast | Previsão de transferência | Tempo estimado entre CD de origem e CD de destino (pedidos de transferência e cross-docking). |
| CrossDockingId | Pedido cross-docking (XD) | CD pode bloquear agendamento de pedidos XD em um período. |
| Yard | Registro de pátio | Uma passagem de um veículo + motorista por um CD, ligada a entregas. |
| Vehicle / VehicleType / VehicleCompany / Driver | Veículo, tipo (capacidade em kg), transportadora, motorista | Cadastros da portaria (gatehouse). |
| Voucher | Comprovante do agendamento | Tem QR Code com o id da entrega, usado no recebimento. |
| NoShow | Não comparecimento | Entrega agendada que expirou sem nada recebido. |

## 3. Entidades principais

Todas são documentos RavenDB (agregados desnormalizados, sem joins). Os campos abaixo são os que importam para o domínio.

**Order (Pedido)**
`ExternalId`, `Status`, `SupplierId`, `WarehouseId`, `CategoryId`, `CrossDockingId`, `BuyerId`, `LeadTime`, `PurchaseType` (Compra ou Transferência), `Lines[]`, `Deliveries[]`, `Invoices[]`, `History[]`, `OriginalOrders[]`.
OrderLine: `ProductId`, `EAN`, `PackSize`, `Quantity`, `CanceledQuantity`, `UnitValue` (Money), fator de conversão e unidade do fornecedor.

**Delivery (Entrega agendada)**
`WarehouseId`, `Suppliers[]`, `Orders[]`, `Lines[]`, `DeliveryDate`, `OriginalDeliveryDate`, `DeliveryInterval`, `Status`, `ReschedulingAmount`, `ToCollect` (coleta), `Manual`, `Immediate`, `Transfer`, `NumberOfCars`, `LoadType` (Dedicada ou Compartilhada), `Observation`, `Permissions[]` (violações aprovadas ou pendentes), `UserGroupHasPermissions[]`, `Invoices[]`, `Vehicles[]` (desnormalizado), `ReceivedAt`, `ReceivedById`, `ScheduledBy`, `RescheduledBy`, `History[]`.
DeliveryLine: `OrderId`, `InvoiceId`, `ProductId`, `Quantity`, `DeliveredQuantity`, `CanceledQuantity`, `SupplierId`, `CategoryId`.
A quantidade "em aberto" de uma linha é sempre `Quantity − Canceled − Delivered`.

**Warehouse (CD)**
`ExternalIds[]`, `Description`, `Address`, `TotalReceivingDocks`, `CapacityTypes[]`, capacidades totais (`UnitCapacity`, `SkuCapacity`, `VolumeCapacity`), `OverflowTolerance` (%), `IntervalsByDay` (dia da semana → lista de `DeliveryWindow`), `Advance` (antecedência mínima, em dias úteis), `ClosingSchedule` (fechamento da agenda, em dias úteis), `MaximumTimeScheduling` (hora limite do dia para agendar), `ProductCategories[]` (capacidade por categoria), `ClosedDays[]`, `BlockCrossDockingOrders` com `Period`.
DeliveryWindow: `Interval`, capacidades por tipo, `OverFlowTolerance`, capacidade por categoria de produto.

**Invoice (NFe)**
`AccessKey`, `Number`, `Series`, `IssueDate`, `TotalAmount`, `Issuer`, `Recipient`, `SupplierId`, `WarehouseId`, `CrossDockingId`, `Orders[]`, `Deliveries[]`, `Products[]` (InvoiceLine com EAN, CFOP, unidade, valores), `Status`, `History[]`.

**Yard (pátio)**
`WarehouseId`, `Status` (status do veículo), `StatusUpdateDate`, `Vehicle` e `Driver` (desnormalizados), `Deliveries[]` (desnormalizadas). Arquivado no checkout.

**Vehicle**: `LicensePlate`, `VehicleCompany`, `VehicleType` (tipo + capacidade em kg), `Status`, `History[]`.
**Driver**: `Name`, `Phone`, `Status`.

**Cadastros de apoio**: Supplier, SupplierGroup (+ `Destinations[]` por CD), AutoLockGroup, Product (+ EANs, fabricante, categoria), Manufacturer, Category, SpecialDate, TransferForecast, User, UserGroup, UsersNotifications (quem recebe cada e-mail ou sino), Notification, Owner, Dashboard.

**Padrões que aparecem em todas as entidades**
- `History[]` embutido com `Date`, `Status`, `StatusColor`, `Type` (enum de mensagem traduzível), `Content` (parâmetros da mensagem) e `Author`. É a trilha de auditoria do sistema.
- Status como *Enumeration class* (id + nome + cor + flag `Open`), não `enum` C#.
- Chaves únicas por "compare-exchange" do RavenDB (`{OwnerId}/Orders/ExternalId/{x}`) para garantir unicidade de ExternalId, e-mail, placa etc.

## 4. Máquinas de estado

**Pedido (OrderStatus)**: Pendente → Agendado → Entregue, ou Cancelado.
É derivado das linhas: se ainda há quantidade pendente (não agendada), fica Pendente; se tudo está agendado, Agendado; se tudo foi recebido ou cancelado, Entregue ou Cancelado.

**Entrega (DeliveryStatus)**: Pendente (aguardando aprovação) → Agendada → Entregue; ou Cancelada (recusada) ou No-show.
- Nasce **Agendada** se não houve violação, ou se o usuário que agendou já tem todas as permissões violadas, ou se o fornecedor está num grupo de **auto-aprovação**.
- Nasce **Pendente** se há violações que o usuário não pode aprovar, ou se o fornecedor está num grupo de **bloqueio**.
- Pendente → Agendada por *Confirmar entrega* (usuário com a permissão). Pendente ou Agendada → Cancelada por *Recusar*.
- Ao **expirar** (rotina `update-expired`): se algo foi recebido ou é manual vira Entregue; se estava Agendada e nada chegou, vira **No-show**; se estava Pendente, vira Cancelada.

**NFe (InvoiceStatus)**: Pendente → Agendada → Entregue; Cancelada; Recusada. Acompanha as entregas que contêm os produtos da nota.

**Veículo no pátio (VehicleStatus)**: Livre → Em trânsito → Recebido → (checkout) Livre. Existe também "Em andamento".
- Agendar entrega com veículos (ou adicionar veículos a uma entrega agendada) cria o registro de Yard e põe o veículo **Em trânsito**.
- No quadro de pátio, o operador move o card para **Recebido**. De Livre só se pode ir para Em trânsito.
- **Checkout** arquiva o Yard e libera o veículo (**Livre**).

## 5. Fluxos principais

### 5.1 Entrada de pedidos (integração)
1. O ERP do cliente cria e atualiza pedidos via API (`POST /orders`, `/orders/bulk`, `PUT /orders/by-external-id/{id}`), autenticando com `X-Api-Key`. Também há importação por planilha na tela *Importar*.
2. Cancelamentos chegam por pedido inteiro, por item ou por linha via `ExternalId`. Cancelar itens já agendados ajusta ou cancela as entregas e notifica.
3. Job diário avisa fornecedores sobre pedidos novos, pedidos perto do lead time e pedidos vencidos.

### 5.2 Agendamento (fluxo central)
Wizard do frontend em 5 etapas: **Seleção de itens → Veículos → Data de entrega → Validação → Resumo**.
1. Usuário (fornecedor, transportadora ou analista) escolhe pedidos e quantidades de um mesmo CD.
2. Escolhe nº de carros, tipo de carga e, opcionalmente, veículos e motoristas (só veículos livres ou a caminho do CD).
3. O frontend consulta datas e janelas disponíveis (`/warehouses/available-delivery-dates`, ocupação por dia).
4. `POST /deliveries/schedule-validation` roda todas as regras e devolve **violações**, cada uma ligada a uma permissão. Nada é bloqueado nessa etapa: a violação vira "precisa de aprovação".
5. `POST /deliveries/confirm-schedule` cria a entrega com status Agendada ou Pendente (ver seção 4), atualiza pedidos, NFes, veículos e pátio, grava histórico e dispara notificações (sino em tempo real e e-mail).
6. Usuários dos grupos com a permissão recebem a pendência e confirmam ou recusam (`/deliveries/confirm`, `/deliveries/refuse`).

**Regras validadas (cada uma é uma permissão aprovável):**
- **Capacidade do CD no dia** (unidades, SKUs, caixas), com tolerância de overflow em %.
- **Capacidade da janela** de horário, idem, e capacidade por **categoria de produto** no CD e na janela.
- **Antecedência**: agendar antes do lead time além do permitido; prazo de **fechamento da agenda** (dias úteis).
- **Hora limite** do dia para agendar (`MaximumTimeScheduling`).
- **Grupo de fornecedores** (fluxo especial): dias e janela permitidos, **capacidade reservada** para o grupo (e reserva expirada), **nº máximo de carros**, antecedência máxima por lead time e por data de entrega.
- **Fornecedor bloqueado** (grupo de bloqueio).
- **Cross-docking bloqueado** no período do CD.
- Datas especiais ou feriados e dias fechados reduzem ou zeram a capacidade.

Variantes: **coleta** (`ToCollect`, o CD busca), **entrega manual** (criada pelo CD, sem pedido de integração, com alertas de overflow), **imediata** e **transferência** entre CDs (pula a validação). **Reagendamento** refaz a validação, incrementa `ReschedulingAmount` e guarda a data original.

### 5.3 Nota fiscal
1. A NFe chega como XML (do fornecedor ou por integração) e é vinculada aos pedidos pela chave de acesso. O handler valida fornecedor, pedidos e produtos (por EAN ou código, com fator de conversão) e trata produtos duplicados.
2. A NFe pode ser usada como ponto de partida para agendar (*Agendamento por nota fiscal*). Se os produtos da nota estão espalhados em outras entregas, o sistema consolida: cancela ou fecha as antigas e move o saldo para a nova.
3. E-mails para "NFe entregue sem agendamento" e "NFe entregue fora do agendado".

### 5.4 Pátio e portaria
1. Portaria cadastra transportadoras, tipos de veículo, veículos e motoristas.
2. Ao agendar com veículo, nasce um Yard por veículo e CD; o veículo fica **Em trânsito**.
3. Tela *Veículos* tem lista e **kanban** (arrastar e soltar) por status; mudar para **Recebido** atualiza veículo, entregas e histórico e notifica.
4. **Checkout** arquiva o Yard e libera o veículo.

### 5.5 Recebimento
1. Na chegada, o CD lê o **voucher** (QR Code com o id ou o rótulo `ENTR…`) e faz *Confirmar recebimento* (`/deliveries/confirm-receipt`, aceita vários ids).
2. Quantidades recebidas são lançadas por item (`/orders/receive`, `/orders/quantity-delivered`) ou pela NFe (`/invoices/receive`). Existe *Revogar recebimento*.
3. Pedido, NFe e pátio são atualizados; webhooks **DeliveryReceived** e **DeliveryCancelled** são enviados ao sistema do cliente ou ao WMS.

### 5.6 Notificações
- **Sino** em tempo real via SignalR (hub `/notifications`, evento `broadcast`), com lista de não lidas.
- **E-mail** via SendGrid com templates.
- Tela *Gerenciar notificações* define, por tenant, quem recebe cada evento (cerca de 30 tipos: agendada, pendente, reagendada, recusada, cancelada, pedido cancelado, NFe fora do agendado, veículo livre, em trânsito, recebido etc.).
- Disparo via `INotification` do MediatR: o handler do comando publica e os handlers de notificação enviam.

### 5.7 Visões e relatórios
Dashboard (entregas no prazo, no-show, total recebido, status de NFe, visão por CD), Ocupação (dia, semana e mês por CD e janela), Busca global, Relatórios exportados em XLSX (pedidos em aberto, itens em aberto, agendamentos, reagendamentos, NFes, usuários ativos).

## 6. Perfis de acesso

Papéis (claims do provedor de identidade): Administrador, Analista, Fornecedor (e Fornecedor de baixo acesso / fornecedor único), CD (Warehouse) e Gerente de CD, Consulta, Operador logístico, Supervisor, Transportadora (e de baixo acesso), Filial, Coletas, Root.
O usuário também é escopado por **fornecedores**, **CDs** e **categorias** que pode ver. As aprovações de exceção são por **grupo de usuários + permissão**, independente do papel.

## 7. Arquitetura do backend

- **.NET 6**, ASP.NET Core Web API, controllers finos por feature (`Api/Features/*`) que só fazem `mediator.Send(...)`. Rotas duplicadas em `api/` e `v2/`.
- **CQRS com MediatR**: `Commands/*Cmd + CmdHandler`, `Queries/*Query + Handler`, `Notifications/*` para efeitos colaterais. Pipeline behaviors de **validação fail-fast** (FluentValidation) e **logging**.
- Resposta padronizada (`Response`) com lista de erros por campo e código (`ERROR_CODE`), mensagens localizadas.
- **Domínio anêmico**: modelos são POCOs; regras ficam nos handlers (alguns com 40 a 75 KB, como validação de agendamento, NFe e atualização de pedido por integração). Há `Functions/*` com cálculos de ocupação e capacidade.
- **Persistência: RavenDB** (documentos), Unit of Work + repositórios por agregado, **índices map-reduce** para buscas, ocupação e relatórios, streams para exportação, compare-exchange para unicidade, archiving de documentos.
- **Multi-tenant** por `OwnerId` vindo do token (`IUserContext`, que também traz e-mail e fuso do usuário para converter datas).
- **i18n** no backend (pt-BR, en-US, es-AR) via resources.
- **Autenticação**: biblioteca própria de identidade, com JWT de um IdentityServer (OIDC) para usuários e **API Key** (`X-Api-Key`, validada no provedor de identidade) para integrações. Swagger com OAuth.
- **Segredos** no Azure Key Vault (certificado cliente do RavenDB).
- **Testes**: projetos xUnit para Application e repositórios RavenDB.

**Serviços separados**
- API principal + hub SignalR.
- `schedulejob`: API com **Quartz.NET** rodando a cada 10 s um `JobsManager` que despacha jobs configuráveis: backlog, notificar pedidos criados ontem, novos pedidos para fornecedores, pré e pós lead time, recebimentos, NFes pendentes, limpeza de arquivos e archiver.
- `jobs`: esboço com **Microsoft Coyote** (atores) para arquivamento de pedidos; está desligado no código.
- Serviço de **webhooks** separado (só aparece pelo frontend: `WEBHOOKS_API/v1`), que registra mensagens enviadas, tentativas, código HTTP e payload. Integra com WMS quando disponível.

## 8. Frontend

- **React 16** com componentes de classe e hooks, **MobX 4** (stores por domínio, decorators, `mobx-persist`), **react-router 5**, **styled-components** + **Semantic UI React** + **react-rainbow-components**, **chart.js**, **@dnd-kit** (kanban do pátio), **oidc-client** (login OIDC), **@aspnet/signalr**, **axios**, **i18next** (pt-BR, en-US, es-AR), **xlsx/alasql** (importação e exportação de planilhas).
- Build com **Webpack 4 + Babel**, testes com **Jest + Enzyme**, ESLint (airbnb) + Prettier.
- Rotas em português: `/pedidos`, `/entregas` (lista, calendário, detalhes, voucher), `/agendamento`, `/agendamento-nota-fiscal`, `/agendamento/entrega-manual`, `/reagendamento`, `/nota-fiscal`, `/veiculos` (lista e kanban), `/ocupacao`, `/dashboard`, `/relatorios`, `/pesquisa`, `/importar`, `/integracoes` (webhooks), `/notificacoes`, `/gerenciar-notificacoes`, `/admin/*`.
- Admin: CDs (janelas e categorias), usuários, grupos de usuários, grupos de fornecedores, grupos de bloqueio e aprovação, datas especiais, previsão de transferência, motoristas, veículos, tipos de veículo, portaria e configurações globais.
- Há versões *Mobile* das telas de pedidos, entregas e NFe (mesmo app, layout responsivo).

## 9. Integrações externas

| Integração | Direção | Como |
|---|---|---|
| ERP do cliente (pedidos, cancelamentos, CDs, produtos, fornecedores) | entra | REST + API Key, endpoints `by-external-id` e `bulk` |
| NFe (XML) | entra | Upload ou API; parse do XML da SEFAZ (emitente, destinatário, transportador, produtos, totais) |
| Provedor de identidade (IdentityServer/OIDC) | ambos | JWT para usuários, validação de API Key, convite e cadastro de usuário |
| SendGrid | sai | E-mails transacionais com templates |
| Webhooks / WMS | sai | Eventos de entrega recebida e cancelada, com histórico de tentativas |
| Application Insights + NLog (Azure Storage) | sai | Telemetria e logs |
| Azure Key Vault | entra | Segredos e certificado do banco |

## 10. Infra e entrega

- Containers Docker (API em `aspnet:6.0`; frontend com build estático servido por **nginx** com template de config).
- **Kubernetes** (manifests de Deployment e Service por ambiente: development, test, production) e imagens num **Azure Container Registry**.
- **Azure Pipelines**: build do frontend por ambiente, docker build e push, publicação dos manifests.
- Observabilidade limitada: Application Insights com quase todos os módulos desligados, NLog, sem métricas de negócio nem tracing distribuído explícito.
- Sem mensageria: efeitos colaterais (notificações, e-mails, pátio) rodam **em processo**, dentro do handler, depois do `SaveChanges`.

## 11. Leitura crítica: o que levar e o que simplificar no YMS

Esta seção é opinião minha a partir do código, para alimentar a spec.

**Vale manter como conceito**
- Entrega como agregado central, com linhas e a conta `Quantity − Canceled − Delivered`.
- Janela de recebimento por dia da semana com capacidade por tipo (unidades, SKUs, volumes) e tolerância.
- "Violação vira aprovação" em vez de bloqueio duro. É a ideia mais interessante do domínio.
- Histórico embutido como trilha de auditoria (no YMS, pode virar *event log* ou eventos de domínio de verdade).
- Status como objeto (id, nome, cor, aberto/fechado).
- Ciclo do veículo no pátio: em trânsito → recebido → checkout.

**Vale simplificar ou cortar no começo**
- Grupos de fornecedores com reserva de capacidade, cross-docking, transferência, coleta, entrega manual, datas especiais e categorias de capacidade: tudo isso pode ficar para depois do MVP.
- NFe: começar só com "número da nota" opcional na entrega; o parse de XML da SEFAZ é um projeto à parte.
- 13 papéis podem virar 3 ou 4 (admin, operador do CD, fornecedor ou transportadora, portaria).

**Onde o YMS pode mostrar o que falta no currículo**
- **Domínio rico**: tirar as regras dos handlers gigantes e pôr nos agregados (Delivery, Warehouse e uma política de capacidade testável).
- **Mensageria**: trocar o "publica em processo depois do SaveChanges" por *outbox* + broker (RabbitMQ ou Azure Service Bus) para notificações, webhooks e pátio.
- **Observabilidade**: OpenTelemetry (traces, métricas, logs) com métricas de negócio como no-show, ocupação e tempo de pátio.
- **DevOps**: GitHub Actions, Docker Compose local, IaC e deploy simples.
- **Pátio de verdade**: hoje não há doca atribuída, tempo de espera, fila nem check-in na portaria com horário; são candidatos naturais ao "Y" do novo YMS.
- **IA aplicada**: sugestão de janela por histórico de ocupação, leitura de NFe e documentos, ou um assistente para o operador do pátio.
