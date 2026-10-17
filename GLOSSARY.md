# YMS - Recebimento

Portal onde Fornecedores agendam a entrega de Pedidos de Compra nos Centros de Distribuição de um Owner, e onde a logística do Owner aprova, recusa e reagenda essas entregas.

## Language

### Participantes

**Owner** (`Owner`):
A empresa compradora, dona dos Centros de Distribuição, que emite Pedidos de Compra e recebe as mercadorias.
_Avoid_: Cliente, Comprador, Empresa

**Centro de Distribuição** (`DistributionCenter`):
Um armazém do Owner onde as mercadorias são recebidas. Abreviado como CD.
_Avoid_: Unidade, Armazém, Filial, Site

**Fornecedor** (`Supplier`):
Uma empresa cadastrada por um Owner, que vende mercadorias a esse Owner e as entrega em seus Centros de Distribuição. A mesma empresa que vende a dois Owners é um Fornecedor diferente em cada um.
_Avoid_: Vendedor, Parceiro, Vínculo

### Integração

**Lote** (`Batch`):
Uma requisição da API do Owner com vários registros do mesmo tipo (Categorias, Produtos, Fornecedores ou Pedidos de Compra), processada de forma assíncrona e com resultado item a item.
_Avoid_: Bulk, Book, Carga, Pacote

### Usuários

Cada usuário tem um único perfil.

**Administrador** (`Admin`):
O usuário do Owner com maior acesso; cadastra os usuários do Owner e dos Fornecedores e define a Carteira de cada um.
_Avoid_: Gestor, Master

**Operador logístico** (`LogisticsOperator`):
O usuário do Owner que aprova, recusa e reagenda Agendamentos.
_Avoid_: Logística, Aprovador

**Operador de pátio** (`YardOperator`):
O usuário do Owner que atua na portaria e na doca, marcando a chegada, o início e o fim da descarga.
_Avoid_: Portaria, Doca, Porteiro

**Carteira** (`Portfolio`):
As listas de Fornecedores, Categorias e Centros de Distribuição que um usuário pode enxergar; cada lista vale "todos" ou itens específicos. Definida pelo Administrador ao cadastrar o usuário.
_Avoid_: Escopo, Permissão, Acesso

### Catálogo

**Categoria** (`Category`):
Uma classificação, sem hierarquia, que o Owner dá aos Produtos e aos Pedidos de Compra; é por ela que a Carteira filtra os Pedidos de Compra.
_Avoid_: Departamento, Seção, Família, Grupo

**Produto** (`Product`):
Uma mercadoria do catálogo do Owner, sempre com uma unidade de medida, que aparece nos itens dos Pedidos de Compra.
_Avoid_: Item, SKU, Material, Mercadoria

### Pedido de Compra

**Pedido de Compra** (`Order`):
Uma compra de uma Categoria, emitida pelo Owner a um Fornecedor, cuja mercadoria será entregue em um Centro de Distribuição.
_Avoid_: Pedido, Ordem, PO, Ordem de Recebimento

**Saldo** (`RemainingQuantity`):
A quantidade de um item do Pedido de Compra que ainda não está em nenhum Agendamento, exceto os Recusados, Cancelados ou No-show.
_Avoid_: Pendente, Restante, Disponível

#### Situações do Pedido de Compra

Calculadas a partir do Saldo e dos Agendamentos; nunca alteradas manualmente, exceto Cancelado.

**Aberto** (`Open`):
Nenhuma quantidade do Pedido de Compra está em Agendamento.

**Parcialmente agendado** (`PartiallyScheduled`):
Parte do Saldo do Pedido de Compra está em Agendamentos.

**Totalmente agendado** (`FullyScheduled`):
O Saldo é zero, mas ainda há Agendamentos não Concluídos.

**Atendido** (`Fulfilled`):
Todas as quantidades estão em Agendamentos Concluídos.
_Avoid_: Recebido, Entregue, Fechado

**Cancelado** (`Cancelled`):
O Owner cancelou o Pedido de Compra.

### Agendamento

**Janela** (`TimeSlot`):
Um intervalo de horário em um Centro de Distribuição com capacidade para um número limitado de Agendamentos.
_Avoid_: Slot, Horário, Turno

**Agendamento** (`Appointment`):
A reserva feita pelo Fornecedor de uma Janela para entregar, em um Centro de Distribuição, quantidades de itens de um ou mais Pedidos de Compra; um Pedido de Compra pode ser entregue em vários Agendamentos.
_Avoid_: Reserva, Marcação, Booking

**Reagendamento** (`Reschedule`):
A troca da Janela de um Agendamento; é uma ação, não um estado.

#### Estados do Agendamento

**Solicitado** (`Requested`):
O Fornecedor pediu uma Janela e aguarda a decisão do Owner.

**Aprovado** (`Approved`):
O Owner aceitou o Agendamento, manualmente ou por aprovação automática; o CD aguarda a chegada do veículo.
_Avoid_: Confirmado, Agendado

**Recusado** (`Rejected`):
O Owner negou o Agendamento, com motivo. Estado final.

**Cancelado** (`Cancelled`):
O Agendamento foi desfeito pelo Fornecedor ou pelo Owner antes da chegada. Estado final.

**Chegou** (`Arrived`):
O veículo se apresentou na portaria do CD.
_Avoid_: Recebido na portaria, Check-in

**Em descarga** (`Unloading`):
O veículo está em uma doca e a descarga começou.
_Avoid_: Em andamento, Em conferência

**Concluído** (`Completed`):
A descarga terminou e o veículo liberou a doca, independentemente do resultado da conferência. Estado final.
_Avoid_: Conferido, Finalizado, Recebido

**No-show** (`NoShow`):
O veículo de um Agendamento Aprovado não se apresentou até o fim da Janela mais a Tolerância do CD. Volta a Chegou se o Owner aceitar a chegada atrasada.
_Avoid_: Não compareceu, Falta

#### Pontualidade

**Tolerância** (`GracePeriod`):
O tempo, configurado por CD, que se espera após o fim da Janela antes de um Agendamento virar No-show.
_Avoid_: Carência, Margem

**Atrasado** (`Late`):
Marcação de um Agendamento cujo veículo chegou após o fim da Janela; não é um estado.
_Avoid_: Fora do horário
