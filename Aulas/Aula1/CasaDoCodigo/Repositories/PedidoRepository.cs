﻿using CasaDoCodigo.Models;
using CasaDoCodigo.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CasaDoCodigo.Repositories
{
    public interface IPedidoRepository
    {
        Pedido GetPedido();
        void AddItem(string codigo);
        UpdateQuantidadeResponse UpdateQuantidade(ItemPedido itemPedido);
    }

    public class PedidoRepository : BaseRepository<Pedido>, IPedidoRepository
    {
        private IHttpContextAccessor contextAccessor { get; }
        private IItemPedidoRepository itemPedidoRepository { get; }

        public PedidoRepository(ApplicationContext contexto, IHttpContextAccessor contextAccessor, IItemPedidoRepository itemPedidoRepository)
            : base(contexto)
        {
            this.contextAccessor = contextAccessor;
            this.itemPedidoRepository = itemPedidoRepository;
        }

        private int? GetPedidoId()
        {
            return contextAccessor.HttpContext.Session.GetInt32("pedidoId");
        }

        private void SetPedidoId(int pedidoId)
        {
            contextAccessor.HttpContext.Session.SetInt32("pedidoId", pedidoId);
        }

        public Pedido GetPedido()
        {
            var pedidoId = GetPedidoId();
            var pedido = dbSet
                .Include(o => o.Itens)
                    .ThenInclude(o => o.Produto)
                .SingleOrDefault(p => p.Id == pedidoId);

            if (pedido == null)
            {
                pedido = new Pedido();
                dbSet.Add(pedido);
                contexto.SaveChanges();

                SetPedidoId(pedido.Id);
            }

            return pedido;
        }

        public void AddItem(string codigo)
        {
            var produto = contexto.Set<Produto>().SingleOrDefault(p => p.Codigo == codigo);

            if (produto == null)
            {
                throw new ArgumentException("Produto não encontrado");
            }

            var pedido = GetPedido();

            var itemPedido = contexto.Set<ItemPedido>().SingleOrDefault(i => i.Produto.Codigo == codigo && i.Pedido.Id == pedido.Id);

            if (itemPedido == null)
            {
                itemPedido = new ItemPedido(pedido, produto, 1, produto.Preco);
                
                contexto.Set<ItemPedido>().Add(itemPedido);
                contexto.SaveChanges();
            }
        }

        public UpdateQuantidadeResponse UpdateQuantidade(ItemPedido itemPedido)
        {
            var itemPedidoDB = itemPedidoRepository.GetItemPedido(itemPedido.Id);

            if (itemPedidoDB == null)
            {
                throw new ArgumentException("ItemPedido não encontrado");
            }

            if (itemPedido.Quantidade <= 0)
            {
                itemPedidoRepository.RemoveItemPedido(itemPedido.Id);
            }

            itemPedidoDB.AtualizaQuantidade(itemPedido.Quantidade);

            contexto.SaveChanges();

            var carrinhoViewModel = new CarrinhoViewModel(GetPedido().Itens);

            return new UpdateQuantidadeResponse(itemPedidoDB, carrinhoViewModel);
        }
    }
}
