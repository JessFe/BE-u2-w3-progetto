﻿using PizzeriaInForno.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace PizzeriaInForno.Controllers
{
    public class OrdiniController : Controller
    {
        private ModelDbContext db = new ModelDbContext();

        // GET: Ordini
        public ActionResult Index()
        {
            // Ottieni tutti gli ordini NON EVASI e ordina per data, vecchi in cima
            var ordiniNonEvasi = db.Ordini.Include(o => o.Utenti)
                                           .Where(o => !o.Evaso.HasValue || !o.Evaso.Value)
                                           .OrderBy(o => o.DataOrdine)
                                           .ToList();

            // Ottieni tutti gli ordini EVASI e ordina per data, recenti in cima
            var ordiniEvasi = db.Ordini.Include(o => o.Utenti)
                                       .Where(o => o.Evaso.HasValue && o.Evaso.Value)
                                       .OrderByDescending(o => o.DataOrdine)
                                       .ToList();

            // Unisci le due liste
            var ordini = ordiniNonEvasi.Concat(ordiniEvasi).ToList();

            return View(ordini);
        }

        // GET Ordini/Evaso/5

        public ActionResult Evaso(int id)
        {
            var ordine = db.Ordini.Find(id);
            if (ordine != null)
            {
                // Cambia lo stato di evasione dell'ordine
                // Se Evaso è null, GetValueOrDefault restituisce false, quindi lo imposteremo su true
                ordine.Evaso = !ordine.Evaso.GetValueOrDefault();

                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return HttpNotFound();
        }

        // GET: Ordini/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ordini ordini = db.Ordini.Find(id);
            if (ordini == null)
            {
                return HttpNotFound();
            }
            return View(ordini);
        }

        // GET: Ordini/Create
        public ActionResult Create()
        {
            ViewBag.FK_IDUtente = new SelectList(db.Utenti, "IDUtente", "Username");
            return View();
        }

        // POST: Ordini/Create
        // Per la protezione da attacchi di overposting, abilitare le proprietà a cui eseguire il binding. 
        // Per altri dettagli, vedere https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "IDOrdine,FK_IDUtente,DataOrdine,Indirizzo,Note,Evaso")] Ordini ordini)
        {
            if (ModelState.IsValid)
            {
                db.Ordini.Add(ordini);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.FK_IDUtente = new SelectList(db.Utenti, "IDUtente", "Username", ordini.FK_IDUtente);
            return View(ordini);
        }

        // GET: Ordini/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ordini ordini = db.Ordini.Find(id);
            if (ordini == null)
            {
                return HttpNotFound();
            }
            ViewBag.FK_IDUtente = new SelectList(db.Utenti, "IDUtente", "Username", ordini.FK_IDUtente);
            return View(ordini);
        }

        // POST: Ordini/Edit/5
        // Per la protezione da attacchi di overposting, abilitare le proprietà a cui eseguire il binding. 
        // Per altri dettagli, vedere https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "IDOrdine,FK_IDUtente,DataOrdine,Indirizzo,Note,Evaso")] Ordini ordini)
        {
            if (ModelState.IsValid)
            {
                db.Entry(ordini).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.FK_IDUtente = new SelectList(db.Utenti, "IDUtente", "Username", ordini.FK_IDUtente);
            return View(ordini);
        }

        // GET: Ordini/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ordini ordini = db.Ordini.Find(id);
            if (ordini == null)
            {
                return HttpNotFound();
            }
            return View(ordini);
        }

        // POST: Ordini/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Ordini ordini = db.Ordini.Find(id);
            db.Ordini.Remove(ordini);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }


        [HttpPost]
        public ActionResult AggiungiAlCarrello(int prodottoId, int quantita)
        {
            var carrello = Session["Carrello"] as List<DettaglioCarrello> ?? new List<DettaglioCarrello>();

            var prodotto = db.Prodotti.Find(prodottoId);
            if (prodotto != null)
            {
                var dettaglioCarrello = new DettaglioCarrello
                {
                    IDProdotto = prodottoId,
                    Quantita = quantita,
                    NomeProd = prodotto.NomeProd,
                    Prezzo = prodotto.Prezzo
                };

                carrello.Add(dettaglioCarrello);
                Session["Carrello"] = carrello;
            }

            return RedirectToAction("Catalogo", "Prodotti");
        }



        // GET: Ordini/Riepilogo
        public ActionResult Riepilogo()
        {
            var carrello = Session["Carrello"] as List<PizzeriaInForno.Models.DettaglioCarrello>;
            if (carrello == null || !carrello.Any())
            {
                // Gestisci il caso in cui il carrello è vuoto
                return RedirectToAction("Catalogo", "Prodotti");
            }

            var model = new SchedaOrdine
            {
                Articoli = carrello

            };

            return View(model);
        }

        // POST: Ordini/Conferma
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Conferma(string indirizzo, string note)
        {
            // Ottiene il carrello dalla sessione
            var carrello = Session["Carrello"] as List<DettaglioCarrello>;

            if (carrello != null && carrello.Any())
            {
                Ordini nuovoOrdine = new Ordini
                {
                    DataOrdine = DateTime.Now,
                    Indirizzo = indirizzo,
                    Note = note,
                    Evaso = false,
                    FK_IDUtente = Convert.ToInt32(Session["UserID"]),
                };

                // Crea i dettagli ordini basandoti sugli articoli nel carrello
                foreach (var item in carrello)
                {
                    nuovoOrdine.DettagliOrdini.Add(new DettagliOrdini
                    {
                        FK_IDProdotto = item.IDProdotto,
                        Quantita = item.Quantita
                    });
                }

                db.Ordini.Add(nuovoOrdine);
                db.SaveChanges();

                // Pulisce il carrello dopo aver salvato l'ordine
                Session["Carrello"] = new List<DettaglioCarrello>();

                // Reindirizza alla vista "ConfermaOrdine" con l'ID dell'ordine appena creato
                return RedirectToAction("ConfermaOrdine", new { id = nuovoOrdine.IDOrdine });
            }

            // In caso di errore, riporta l'utente alla pagina di riepilogo per correzioni
            return View("Riepilogo", carrello);
        }

        public ActionResult ConfermaOrdine(int id)
        {
            var ordine = db.Ordini.Include(o => o.Utenti).Include(o => o.DettagliOrdini.Select(d => d.Prodotti)).FirstOrDefault(o => o.IDOrdine == id);
            if (ordine == null)
            {
                return HttpNotFound();
            }

            return View(ordine);
        }

        [HttpPost]
        public ActionResult AggiornaCarrello(SchedaOrdine model)
        {
            var carrelloAggiornato = new List<DettaglioCarrello>();

            foreach (var articolo in model.Articoli)
            {
                var prodotto = db.Prodotti.Find(articolo.IDProdotto);
                if (prodotto != null)
                {
                    carrelloAggiornato.Add(new DettaglioCarrello
                    {
                        IDProdotto = articolo.IDProdotto,
                        Quantita = articolo.Quantita,
                        NomeProd = prodotto.NomeProd,
                        Prezzo = prodotto.Prezzo
                    });
                }
            }

            Session["Carrello"] = carrelloAggiornato;

            return RedirectToAction("Riepilogo");
        }

        [HttpPost]
        public ActionResult RimuoviDalCarrello(int index)
        {
            var carrello = Session["Carrello"] as List<DettaglioCarrello>;
            if (carrello != null && index >= 0 && index < carrello.Count)
            {
                carrello.RemoveAt(index);
                Session["Carrello"] = carrello;
            }

            return RedirectToAction("Riepilogo");
        }



    }

}
