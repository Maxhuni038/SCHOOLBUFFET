(function (global) {
  var SB = global.SB = global.SB || {};
  var A = SB.A;

  var inv = { search: '', filter: '' };
  var pay = { from: SB.dayKey(-13), to: SB.dayKey(0), status: '', page: 1, list: [] };

  var STOCK_TONE = {
    Mbaruar: 'pill-red',
    IUlet: 'pill-amber',
    NeDorze: 'pill-green',
    PaGjurmim: 'pill'
  };

  var PAY_TONE = {
    Paguar: 'pill-green',
    NePaguar: 'pill-amber',
    Anuluar: 'pill-red'
  };

  var ORDER_TONE = {
    NePritje: 'pill-amber',
    DukeUPergatitur: 'pill-blue',
    Gati: 'pill-green',
    Perfunduar: 'pill-pink'
  };

  function statLine(label, value) {
    return '<div class="stat-line"><span>' + SB.esc(label) + '</span><b>' + SB.esc(value) + '</b></div>';
  }

  function kpi(tone, icon, label, value, note) {
    return '<div class="card kpi ' + tone + '">' +
      '<span class="kpi-icon">' + SB.icon(icon) + '</span>' +
      '<div class="grow"><small>' + SB.esc(label) + '</small><b>' + SB.esc(value) + '</b>' +
      '<span class="note">' + SB.esc(note) + '</span></div>' +
    '</div>';
  }

  function option(value, label, current) {
    return '<option value="' + SB.esc(value) + '"' + (value === current ? ' selected' : '') + '>' + SB.esc(label) + '</option>';
  }

  A.views.inventory = {
    title: 'Inventari',
    loadingText: 'Duke ngarkuar gjendjen e produkteve',
    skeleton: 3,
    render: function (host) {
      A.defineReload(function () {
        loadInventory(host);
      });

      loadInventory(host);
    }
  };

  function inventoryUrl() {
    return 'api/admin/inventory?search=' + encodeURIComponent(inv.search) +
      '&filter=' + encodeURIComponent(inv.filter);
  }

  function stockRow(item) {
    var tone = STOCK_TONE[item.statusKey] || 'pill';
    var stock = item.trackStock
      ? '<td class="num"><b>' + SB.number(item.stockQuantity) + '</b>' +
        '<div class="tiny muted">praggi ' + SB.number(item.lowStockThreshold) + '</div></td>'
      : '<td class="num muted">Pa gjurmim</td>';

    return '<tr>' +
      '<td><b>' + SB.esc(item.productName) + '</b>' +
      '<div class="tiny muted">' + (item.isActive ? 'Aktiv ne meny' : 'Naaktiv') + '</div></td>' +
      '<td class="muted nowrap">' + SB.esc(item.categoryName) + '</td>' +
      '<td><span class="pill ' + tone + '">' + SB.esc(item.statusLabel) + '</span></td>' +
      stock +
      '<td class="num">' + SB.number(item.soldToday) + '</td>' +
      '<td class="num">' + SB.moneyStrict(item.price) + '</td>' +
      '<td class="num nowrap">' +
        '<button class="btn btn-soft btn-sm" type="button" data-inv="adjust" data-id="' + item.productId + '">' +
        SB.icon('plus', 'icon-sm') + 'Ndrysho</button> ' +
        '<button class="btn btn-ghost btn-sm" type="button" data-inv="moves" data-id="' + item.productId + '">' +
        SB.icon('list', 'icon-sm') + 'Levizjet</button>' +
      '</td></tr>';
  }

  function paintInventory(host, list) {
    var summary = list.summary;

    A.setSub(summary.totalProducts + ' produkte · ' + SB.number(summary.unitsOnHand) + ' copeke ne dorze · ' +
      SB.moneyStrict(summary.valueOnHand) + ' vlere ne inventar');

    A.setActions([
      {
        id: 'reload',
        label: 'Rifresko',
        className: 'btn-ghost',
        icon: 'refresh',
        onClick: function () { loadInventory(host); }
      }
    ]);

    host.innerHTML = '<div class="stack" data-inv-root>' +
      '<div class="kpis">' +
        kpi('kpi-hero', 'box', 'Ne dorze', SB.number(summary.unitsOnHand), SB.moneyStrict(summary.valueOnHand) + ' vlere') +
        kpi('kpi-amber', 'warning', 'Sasi e ulet', SB.number(summary.lowStock), 'Arri pragun kritik') +
        kpi('kpi-red', 'close', 'Mbaruar', SB.number(summary.outOfStock), 'Asnje copeke ne stok') +
        kpi('kpi-blue', 'receipt', 'Shitur sot', SB.number(summary.soldToday), 'Copeke nga porosite') +
        kpi('kpi-green', 'check', 'Te gjurmuar', summary.trackedProducts + ' / ' + summary.totalProducts,
          (summary.totalProducts - summary.trackedProducts) + ' produkte pa gjurmim') +
      '</div>' +
      '<div class="filter-bar">' +
        '<label class="field grow"><span class="tiny strong">Kerko produkt</span>' +
        '<input class="input" id="invSearch" type="search" placeholder="P.sh. Sandwich" value="' + SB.esc(inv.search) + '"></label>' +
        '<label class="field"><span class="tiny strong">Gjendja</span>' +
        '<select class="select" id="invFilter">' +
          option('', 'Te gjitha gjendjet', inv.filter) +
          option('iulet', 'Sasi e ulet', inv.filter) +
          option('mbaruar', 'Mbaruar', inv.filter) +
          option('neDorze', 'Ne dorze', inv.filter) +
          option('pagjurmim', 'Pa gjurmim', inv.filter) +
        '</select></label>' +
        '<button class="btn btn-soft" type="button" id="invApply">' + SB.icon('filter', 'icon-sm') + 'Apliko</button>' +
      '</div>' +
      (list.items.length
        ? '<div class="table-wrap"><table class="data"><thead><tr><th>Produkti</th><th>Kategoria</th>' +
          '<th>Gjendja</th><th class="num">Ne dorze</th><th class="num">Shitur sot</th><th class="num">Çmimi</th>' +
          '<th class="num">Veprime</th></tr></thead><tbody>' + list.items.map(stockRow).join('') + '</tbody></table></div>'
        : A.empty('box', 'Asnje produkt nuk perputhet me filtrin e zgjedhur.')) +
      '<p class="note-box">' + SB.icon('info', 'icon-sm') +
      'Sasite zvogelohen automatikisht kur krijohet porosia dhe kthehen mbrapsht kur porosia anulohet ose fshihet. ' +
      'Ndryshimet manuale regjistrohen ne librin e levizjeve me daten, arsyen dhe perdoruesin.</p>' +
    '</div>';

    bindInventoryRoot(host, list);
  }

  function bindInventoryRoot(host, list) {
    var root = host.querySelector('[data-inv-root]');

    root.querySelector('#invApply').addEventListener('click', function () {
      inv.search = root.querySelector('#invSearch').value.trim();
      inv.filter = root.querySelector('#invFilter').value;
      loadInventory(host);
    });

    root.querySelector('#invSearch').addEventListener('keydown', function (event) {
      if (event.key === 'Enter') {
        event.preventDefault();
        root.querySelector('#invApply').click();
      }
    });

    root.addEventListener('click', function (event) {
      var button = event.target.closest('[data-inv]');

      if (!button) {
        return;
      }

      var id = Number(button.getAttribute('data-id'));
      var item = list.items.filter(function (row) {
        return row.productId === id;
      })[0];

      if (!item) {
        return;
      }

      if (button.getAttribute('data-inv') === 'adjust') {
        openAdjust(item, host);
        return;
      }

      openMovements(item);
    });
  }

  function loadInventory(host) {
    return SB.api.get(inventoryUrl()).then(function (list) {
      paintInventory(host, list);
    }).catch(function (error) {
      A.handleError(error, host);
    });
  }

  function openAdjust(item, host) {
    var html = '<form id="adjustForm" class="stack">' +
      '<div class="stat-list">' +
        statLine('Produkti', item.productName) +
        statLine('Gjendja aktuale', item.statusLabel) +
        statLine('Sasia ne dorze', SB.number(item.stockQuantity) + ' copeke') +
        statLine('Praggi i ulete', SB.number(item.lowStockThreshold)) +
      '</div>' +
      '<div class="field"><label for="adjChange">Ndryshimi i sases</label>' +
        '<input class="input" id="adjChange" name="Change" type="number" step="1" value="10">' +
        '<div class="tiny muted">Pozitiv per shtim, negativ per zbritje. Rezultati nuk mund te jete nen zero.</div>' +
        '<span class="field-error"></span></div>' +
      '<div class="field"><label for="adjReason">Arsyeja</label>' +
        '<select class="select" id="adjReason" name="Reason">' +
          '<option value="Shtim manual">Shtim manual</option>' +
          '<option value="Zbritje manual">Zbritje manual</option>' +
          '<option value="Korrektim inventari">Korrektim inventari</option>' +
        '</select><span class="field-error"></span></div>' +
      '<div class="field"><label for="adjNote">Shenim</label>' +
        '<textarea class="textarea" id="adjNote" name="Note" rows="2" maxlength="24" placeholder="P.sh. inventari i dates"></textarea>' +
        '<div class="tiny muted">Deri 24 shkronja, ruhet ne librin e levizjeve.</div>' +
        '<span class="field-error"></span></div>' +
    '</form>';

    SB.modal({
      title: 'Ndrysho sasen',
      subtitle: item.productName,
      html: html,
      actions: [
        { id: 'cancel', label: 'Anulo', className: 'btn-ghost' },
        { id: 'save', label: 'Ruaj ndryshimin' }
      ],
      onAction: function (id, ref) {
        if (id !== 'save') {
          ref.close();
          return;
        }

        var form = ref.root.querySelector('#adjustForm');
        var button = ref.root.querySelector('[data-action="save"]');
        var change = Number(form.querySelector('#adjChange').value);

        if (!change) {
          SB.toast('Vendos nje ndryshim qe nuk eshte zero', 'error');
          return;
        }

        SB.busy(button, true, 'Duke ruajtur');

        SB.api.patch('api/admin/inventory/' + item.productId + '/adjust', {
          change: change,
          reason: form.querySelector('#adjReason').value,
          note: form.querySelector('#adjNote').value.trim()
        }).then(function (updated) {
          ref.close();
          SB.toast(updated.productName + ' tani ka ' + updated.stockQuantity + ' copeke.', 'success');
          loadInventory(host);
        }).catch(function (error) {
          SB.busy(button, false);

          if (error.errors) {
            SB.applyErrors(form, error.errors);
          }

          SB.toast(error.message, 'error');
        });
      }
    });
  }

  function openMovements(item) {
    SB.api.get('api/admin/inventory/movements?productId=' + item.productId + '&pageSize=12')
      .then(function (data) {
        var body = data.movements.length
          ? '<div class="table-wrap"><table class="data"><thead><tr><th>Koha</th><th>Lloji</th>' +
            '<th class="num">Ndryshimi</th><th class="num">Pas</th><th>Referenca</th><th>Kush</th></tr></thead><tbody>' +
            data.movements.map(function (move) {
              return '<tr><td class="nowrap">' + SB.esc(SB.dateTime(move.createdAtUtc)) + '</td>' +
                '<td>' + SB.esc(move.reason) + '</td>' +
                '<td class="num strong">' + (move.change > 0 ? '+' : '') + move.change + '</td>' +
                '<td class="num">' + move.stockAfter + '</td>' +
                '<td class="muted nowrap">' + SB.esc(move.reference || '-') + '</td>' +
                '<td class="muted nowrap">' + SB.esc(move.user || 'Sistem') + '</td></tr>';
            }).join('') + '</tbody></table></div>'
          : A.empty('list', 'Ky produkt nuk ka levizje te regjistruara.');

        SB.modal({
          title: 'Levizjet e inventarit',
          subtitle: item.productName + ' · ' + data.total + ' regjistrime gjithsej',
          html: body
        });
      }).catch(function (error) {
        SB.toast(error.message, 'error');
      });
  }

  A.views.payments = {
    title: 'Pagesat',
    loadingText: 'Duke ngarkuar pagesat e porosive',
    skeleton: 3,
    render: function (host) {
      A.defineReload(function () {
        loadPayments(host);
      });

      loadPayments(host);
    }
  };

  function paymentsUrl() {
    return 'api/admin/payments?from=' + encodeURIComponent(pay.from) +
      '&to=' + encodeURIComponent(pay.to) +
      '&status=' + encodeURIComponent(pay.status) +
      '&page=' + pay.page + '&pageSize=25';
  }

  function paymentRow(payment) {
    var acts = '';

    if (payment.statusKey !== 'Paguar') {
      acts += '<button class="btn btn-sm" type="button" data-pay="paid" data-id="' + payment.id + '">' +
        SB.icon('check', 'icon-sm') + 'Sheno e paguar</button> ';
    }

    if (payment.statusKey === 'Paguar') {
      acts += '<button class="btn btn-ghost btn-sm" type="button" data-pay="pending" data-id="' + payment.id + '">' +
        SB.icon('clock', 'icon-sm') + 'Kthe ne pagese</button> ';
    }

    if (payment.statusKey !== 'Anuluar') {
      acts += '<button class="btn btn-danger btn-sm" type="button" data-pay="void" data-id="' + payment.id + '">' +
        SB.icon('close', 'icon-sm') + 'Anulo</button>';
    }

    return '<tr><td><span class="order-code">' + SB.esc(payment.orderCode) + '</span></td>' +
      '<td>' + SB.esc(payment.studentName) +
      '<div class="tiny muted">' + SB.esc(payment.className) + ' · ' + SB.esc(SB.dateTime(payment.orderCreatedAtUtc)) + '</div></td>' +
      '<td><span class="pill ' + (ORDER_TONE[payment.orderStatusKey] || 'pill') + '">' +
      SB.esc(payment.orderStatusLabel) + '</span></td>' +
      '<td class="num nowrap">' + SB.moneyStrict(payment.amount) + '</td>' +
      '<td class="nowrap">' + SB.esc(payment.methodLabel) + '</td>' +
      '<td><span class="pill ' + (PAY_TONE[payment.statusKey] || 'pill') + '">' + SB.esc(payment.statusLabel) + '</span>' +
      (payment.paidAtUtc
        ? '<div class="tiny muted">' + SB.esc(SB.time(payment.paidAtUtc)) +
          (payment.paidBy ? ' · ' + SB.esc(payment.paidBy) : '') + '</div>'
        : (payment.note ? '<div class="tiny muted">' + SB.esc(payment.note) + '</div>' : '')) + '</td>' +
      '<td class="num nowrap">' + acts + '</td></tr>';
  }

  function paintPayments(host, list) {
    var totals = list.totals;
    var pages = Math.max(1, Math.ceil(list.total / list.pageSize));

    A.setSub('Intervali ' + SB.plainDay(list.from) + ' deri ' + SB.plainDay(list.to) + ' · ' +
      list.total + ' pagese');

    A.setActions([
      { id: 'today', label: 'Sot', className: 'btn-ghost', onClick: function () { setPayRange(0, 0, host); } },
      { id: 'week', label: '7 dite', className: 'btn-ghost', onClick: function () { setPayRange(-6, 0, host); } },
      { id: 'month', label: '30 dite', className: 'btn-ghost', onClick: function () { setPayRange(-29, 0, host); } },
      { id: 'reload', label: 'Rifresko', className: 'btn-ghost', icon: 'refresh', onClick: function () { loadPayments(host); } }
    ]);

    host.innerHTML = '<div class="stack" data-pay-root>' +
      '<div class="kpis">' +
        kpi('kpi-hero', 'wallet', 'Mbledhur', SB.moneyStrict(totals.collected), totals.paidCount + ' pagese te mira') +
        kpi('kpi-amber', 'clock', 'Ne pagese', SB.moneyStrict(totals.pending), totals.pendingCount + ' ne pritje') +
        kpi('kpi-blue', 'receipt', 'Pritet gjithsej', SB.moneyStrict(totals.expected), 'E mbledhur plus ne pagese') +
        kpi('kpi-red', 'close', 'Te anuluara', SB.number(totals.voidCount), 'Nuk numerohen si te ardhura') +
      '</div>' +
      '<div class="filter-bar">' +
        '<label class="field grow"><span class="tiny strong">Nga data</span>' +
        '<input class="input" id="payFrom" type="date" value="' + SB.esc(list.from) + '"></label>' +
        '<label class="field grow"><span class="tiny strong">Deri me</span>' +
        '<input class="input" id="payTo" type="date" value="' + SB.esc(list.to) + '"></label>' +
        '<label class="field"><span class="tiny strong">Statusi</span>' +
        '<select class="select" id="payStatus">' +
          option('', 'Te gjitha', pay.status) +
          option('NePaguar', 'Ne pagese', pay.status) +
          option('Paguar', 'E paguar', pay.status) +
          option('Anuluar', 'E anuluar', pay.status) +
        '</select></label>' +
        '<button class="btn btn-soft" type="button" id="payApply">' + SB.icon('filter', 'icon-sm') + 'Apliko</button>' +
      '</div>' +
      (list.payments.length
        ? '<div class="table-wrap"><table class="data"><thead><tr><th>Kodi</th><th>Porosia</th>' +
          '<th>Statusi i porosise</th><th class="num">Shuma</th><th>Metoda</th><th>Pagesa</th>' +
          '<th class="num">Veprime</th></tr></thead><tbody>' +
          list.payments.map(paymentRow).join('') + '</tbody></table></div>'
        : A.empty('wallet', 'Asnje pagese ne kete interval.')) +
      '<div class="row wrap">' +
        '<button class="btn btn-ghost btn-sm" type="button" id="payPrev"' + (list.page <= 1 ? ' disabled' : '') + '>Faqja para</button>' +
        '<span class="tiny muted grow center">Faqja ' + list.page + ' / ' + pages + ' · ' + list.total + ' regjistrime</span>' +
        '<button class="btn btn-ghost btn-sm" type="button" id="payNext"' + (list.page >= pages ? ' disabled' : '') + '>Faqja mbas</button>' +
      '</div>' +
      '<p class="note-box">' + SB.icon('info', 'icon-sm') +
      'Pagesa hapet automatikisht sapo krijohet porosia dhe shenohet e paguar kur porosia dorzohet. ' +
      'Keto veprime perdoren per perndjehtim manual ose korrigjim.</p>' +
    '</div>';

    bindPaymentsRoot(host);
  }

  function findPayment(id) {
    return pay.list.filter(function (row) {
      return row.id === id;
    })[0] || null;
  }

  function bindPaymentsRoot(host) {
    var root = host.querySelector('[data-pay-root]');

    root.querySelector('#payApply').addEventListener('click', function () {
      pay.from = root.querySelector('#payFrom').value || pay.from;
      pay.to = root.querySelector('#payTo').value || pay.to;
      pay.status = root.querySelector('#payStatus').value;
      pay.page = 1;
      loadPayments(host);
    });

    root.querySelector('#payPrev').addEventListener('click', function () {
      pay.page = Math.max(1, pay.page - 1);
      loadPayments(host);
    });

    root.querySelector('#payNext').addEventListener('click', function () {
      pay.page = pay.page + 1;
      loadPayments(host);
    });

    root.addEventListener('click', function (event) {
      var button = event.target.closest('[data-pay]');

      if (!button) {
        return;
      }

      var id = Number(button.getAttribute('data-id'));
      var act = button.getAttribute('data-pay');

      if (act === 'paid') {
        openMarkPaid(findPayment(id), host);
        return;
      }

      askPaymentChange(id, act, host, button);
    });
  }

  function openMarkPaid(payment, host) {
    if (!payment) {
      SB.toast('Rreshti i pageses nuk u gjet. Rifresko liste.', 'error');
      return;
    }

    var html = '<form id="paidForm" class="stack">' +
      '<div class="stat-list">' +
        statLine('Porosia', payment.orderCode) +
        statLine('Studenti', payment.studentName + ' · ' + payment.className) +
        statLine('Shuma', SB.moneyStrict(payment.amount)) +
        statLine('Statusi i porosise', payment.orderStatusLabel) +
      '</div>' +
      '<div class="field"><label for="paidMethod">Metoda e pageses</label>' +
        '<select class="select" id="paidMethod" name="Method">' +
          '<option value="KarteKantine">Karte kantina</option>' +
          '<option value="ParaMeDore">Para me dore</option>' +
          '<option value="Tjetra">Tjetra</option>' +
        '</select><span class="field-error"></span></div>' +
      '<div class="field"><label for="paidNote">Shenim</label>' +
        '<textarea class="textarea" id="paidNote" name="Note" rows="2" placeholder="P.sh. pagesa u verifikua nga karta"></textarea>' +
        '<span class="field-error"></span></div>' +
    '</form>';

    SB.modal({
      title: 'Sheno pagesen e paguar',
      subtitle: payment.orderCode,
      html: html,
      actions: [
        { id: 'cancel', label: 'Anulo', className: 'btn-ghost' },
        { id: 'save', label: 'Ruaj pagesen' }
      ],
      onAction: function (id, ref) {
        if (id !== 'save') {
          ref.close();
          return;
        }

        var form = ref.root.querySelector('#paidForm');
        var button = ref.root.querySelector('[data-action="save"]');
        SB.busy(button, true, 'Duke ruajtur');

        SB.api.patch('api/admin/payments/' + payment.id + '/paid', {
          method: form.querySelector('#paidMethod').value,
          note: form.querySelector('#paidNote').value.trim()
        }).then(function (updated) {
          ref.close();
          SB.toast('Pagesa per ' + updated.orderCode + ' u shenua e paguar.', 'success');
          loadPayments(host);
        }).catch(function (error) {
          SB.busy(button, false);

          if (error.errors) {
            SB.applyErrors(form, error.errors);
          }

          SB.toast(error.message, 'error');
        });
      }
    });
  }

  function askPaymentChange(id, act, host, button) {
    var voiding = act === 'void';

    SB.confirm({
      title: voiding ? 'Anulo pagesen' : 'Kthe ne pagese',
      text: voiding
        ? 'Pagesa do te shenohet e anuluar. Kjo nuk mbyll dhe nuk rihap porosine.'
        : 'Pagesa do te kthehet ne gjendjen ne pagese.',
      danger: voiding,
      okLabel: 'Po, vijolo'
    }).then(function (ok) {
      if (!ok) {
        return;
      }

      SB.busy(button, true, 'Duke ruajtur');

      SB.api.patch('api/admin/payments/' + id + '/' + act, { note: '' }).then(function (updated) {
        SB.busy(button, false);
        SB.toast(updated.orderCode + ': ' + updated.statusLabel, 'success');
        loadPayments(host);
      }).catch(function (error) {
        SB.busy(button, false);
        SB.toast(error.message, 'error');
      });
    });
  }

  function setPayRange(fromOffset, toOffset, host) {
    pay.from = SB.dayKey(fromOffset);
    pay.to = SB.dayKey(toOffset);
    pay.page = 1;
    loadPayments(host);
  }

  function loadPayments(host) {
    return SB.api.get(paymentsUrl()).then(function (list) {
      pay.list = list.payments;
      paintPayments(host, list);
    }).catch(function (error) {
      A.handleError(error, host);
    });
  }
})(window);
