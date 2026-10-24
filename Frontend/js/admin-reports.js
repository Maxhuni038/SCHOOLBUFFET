(function (global) {
  var SB = global.SB = global.SB || {};
  var A = SB.A;

  var sales = {
    from: SB.dayKey(-13),
    to: SB.dayKey(0)
  };

  function statLine(label, value) {
    return '<div class="stat-line"><span>' + SB.esc(label) + '</span><b>' + SB.esc(value) + '</b></div>';
  }

  function dayTable(days) {
    var rows = days.slice().reverse();

    return '<div class="table-wrap"><table class="data"><thead><tr><th>Data</th><th>Dita</th>' +
      '<th class="num">Porosi</th><th class="num">Artikuj</th><th class="num">Te ardhurat</th></tr></thead><tbody>' +
      rows.map(function (day) {
        return '<tr><td>' + SB.esc(SB.plainDay(day.date)) + '</td><td>' + SB.esc(SB.weekday(day.date + 'T12:00:00')) +
          '</td><td class="num">' + SB.number(day.orders) + '</td><td class="num">' + SB.number(day.items) +
          '</td><td class="num">' + SB.moneyStrict(day.revenue) + '</td></tr>';
      }).join('') + '</tbody></table></div>';
  }

  function orderTable(orders) {
    if (!orders.length) {
      return A.empty('receipt', 'Asnje porosie ne kete interval dates.');
    }

    return '<div class="table-wrap"><table class="data"><thead><tr><th>Kodi</th><th>Regjistruar</th><th>Studenti</th>' +
      '<th>Klasa</th><th>Statusi</th><th class="num">Artikuj</th><th class="num">Totali</th></tr></thead><tbody>' +
      orders.slice(0, 60).map(function (order) {
        var meta = SB.statusMeta(order.statusKey);

        return '<tr><td><span class="order-code">' + SB.esc(order.code) + '</span></td>' +
          '<td>' + SB.esc(SB.dateTime(order.createdAtUtc)) + '</td>' +
          '<td>' + SB.esc(order.studentName) + '</td><td>' + SB.esc(order.className) + '</td>' +
          '<td><span class="pill ' + meta.pill + '">' + SB.esc(order.statusLabel) + '</span></td>' +
          '<td class="num">' + order.itemCount + '</td><td class="num">' + SB.moneyStrict(order.totalAmount) + '</td></tr>';
      }).join('') + '</tbody></table></div>' +
      (orders.length > 60
        ? '<p class="tiny muted mt">Tabelat shfaqin 60 porosite e fundit. Eksporto CSV per dokumentin e plote.</p>'
        : '');
  }

  function paintSales(host, history) {
    var average = history.orders ? history.revenue / history.orders : 0;
    var days = history.days;
    var best = days.reduce(function (top, day) {
      return day.revenue > top.revenue ? day : top;
    }, days[0] || { date: '', revenue: 0, orders: 0 });

    host.innerHTML = '<div class="stack" data-sales-root>' +
      '<div class="filter-bar">' +
        '<label class="field grow"><span class="tiny strong">Nga data</span>' +
        '<input class="input" type="date" id="salesFrom" value="' + SB.esc(history.from) + '"></label>' +
        '<label class="field grow"><span class="tiny strong">Deri me</span>' +
        '<input class="input" type="date" id="salesTo" value="' + SB.esc(history.to) + '"></label>' +
        '<button class="btn btn-soft" type="button" id="applyRange">' + SB.icon('filter', 'icon-sm') + 'Apliko</button>' +
      '</div>' +
      '<div class="stat-list">' +
        statLine('Intervali', SB.plainDay(history.from) + ' deri ' + SB.plainDay(history.to)) +
        statLine('Te ardhurat', SB.moneyStrict(history.revenue)) +
        statLine('Porosite', SB.number(history.orders) + ' · ' + SB.number(history.items) + ' artikuj') +
        statLine('Mesatarja per porosi', SB.moneyStrict(average)) +
        statLine('Dita me e forte', best ? SB.plainDay(best.date) + ' · ' + SB.moneyStrict(best.revenue) : 'Pa te dhenat') +
      '</div>' +
      '<div class="card card-pad"><div class="panel-title"><h2>Trendi i intervalit</h2>' +
      '<span class="tiny muted">' + days.length + ' dite</span></div>' +
      '<div class="chart-bars" id="salesChart"></div></div>' +
      dayTable(days) +
      '<div class="panel-title section-gap"><h2>Detajet e porosive</h2>' +
      '<a class="btn btn-ghost btn-sm" href="' + exportUrl(history.from, history.to) + '">' +
      SB.icon('download', 'icon-sm') + 'Shkarko CSV</a></div>' +
      orderTable(history.ordersDetail) +
    '</div>';

    SB.charts.bars(document.getElementById('salesChart'), days.map(function (day) {
      return {
        label: SB.plainShort(day.date),
        value: Number(day.revenue) || 0,
        title: SB.plainDay(day.date) + ' · ' + SB.moneyStrict(day.revenue) + ' · ' + day.orders + ' porosi',
        date: day.date
      };
    }), {
      format: function (value) {
        return SB.moneyStrict(value);
      }
    });

    var root = host.querySelector('[data-sales-root]');

    root.querySelector('#applyRange').addEventListener('click', function () {
      sales.from = root.querySelector('#salesFrom').value;
      sales.to = root.querySelector('#salesTo').value;
      loadSales(host);
    });
  }

  function exportUrl(from, to) {
    return 'api/admin/reports/export.csv?from=' + encodeURIComponent(from) + '&to=' + encodeURIComponent(to);
  }

  function loadSales(host) {
    return SB.api.get('api/admin/reports/history?from=' + encodeURIComponent(sales.from) +
      '&to=' + encodeURIComponent(sales.to)).then(function (history) {
      A.setSub('Historiku i shitjeve · ' + SB.plainDay(history.from) + ' deri ' + SB.plainDay(history.to));
      paintSales(host, history);
    }).catch(function (error) {
      A.handleError(error, host);
    });
  }

  A.views.sales = {
    title: 'Shitjet',
    skeleton: 3,
    render: function (host) {
      A.setActions([
        { id: 'today', label: 'Sot', className: 'btn-ghost', onClick: function () { setRange(0, 0, host); } },
        { id: 'week', label: '7 dite', className: 'btn-ghost', onClick: function () { setRange(-6, 0, host); } },
        { id: 'month', label: '30 dite', className: 'btn-ghost', onClick: function () { setRange(-29, 0, host); } },
        {
          id: 'csv',
          label: 'Eksporto CSV',
          className: 'btn-soft',
          icon: 'download',
          onClick: function () {
            global.location.href = exportUrl(sales.from, sales.to);
          }
        },
        { id: 'reload', label: 'Rifresko', className: 'btn-ghost', icon: 'refresh', onClick: function () { loadSales(host); } }
      ]);

      loadSales(host);
    }
  };

  function setRange(fromOffset, toOffset, host) {
    sales.from = SB.dayKey(fromOffset);
    sales.to = SB.dayKey(toOffset);
    loadSales(host);
  }

  A.views.settings = {
    title: 'Cilesite',
    skeleton: 2,
    render: function (host) {
      A.setSub('Llogaria, gjendja e sistemit dhe ndryshimi i fjalakalimit');
      A.setActions([
        { id: 'reload', label: 'Rifresko statusin', className: 'btn-ghost', icon: 'refresh', onClick: function () { loadSettings(host); } }
      ]);

      loadSettings(host);
    }
  };

  function passwordCard() {
    return '<div class="card card-pad"><div class="panel-title"><h2>Ndrysho fjalakalimin</h2>' +
      '<span class="tiny muted">Rregulli: te pakten 10 karaktere, shkronje dhe numer</span></div>' +
      '<form id="passwordForm">' +
        '<div class="field"><label for="CurrentPassword">Fjalakalimi aktual</label>' +
          '<input class="input" id="CurrentPassword" name="CurrentPassword" type="password" autocomplete="current-password">' +
          '<span class="field-error"></span></div>' +
        '<div class="field"><label for="NewPassword">Fjalakalimi i ri</label>' +
          '<input class="input" id="NewPassword" name="NewPassword" type="password" autocomplete="new-password">' +
          '<span class="field-error"></span></div>' +
        '<div class="field"><label for="RepeatPassword">Perserit fjalakalimin e ri</label>' +
          '<input class="input" id="RepeatPassword" name="RepeatPassword" type="password" autocomplete="new-password">' +
          '<span class="field-error"></span></div>' +
        '<button class="btn" type="submit" id="savePassword">' + SB.icon('lock', 'icon-sm') + 'Ruaj fjalakalimin</button>' +
      '</form></div>';
  }

  function loadSettings(host) {
    return SB.api.get('api/health').then(function (health) {
      var me = A.me || { fullName: '', username: '', role: '', lastLoginUtc: null };

      host.innerHTML = '<div class="stack" data-settings-root>' +
        '<div class="panels">' +
          '<div class="card card-pad"><div class="panel-title"><h2>Llogaria e administratorit</h2></div>' +
            '<div class="stat-list">' +
              statLine('Emri', me.fullName) +
              statLine('Perdoruesi', me.username) +
              statLine('Roli', me.role === 'Admin' ? 'Administrator i byfese' : me.role) +
              statLine('Hyrja e fundit', me.lastLoginUtc ? SB.dateTime(me.lastLoginUtc) + ' · ' + SB.relative(me.lastLoginUtc) : 'Pa te dhenat') +
            '</div>' +
            '<div class="row wrap mt">' +
              '<button class="btn btn-ghost" type="button" id="logoutNow">' + SB.icon('logout', 'icon-sm') + 'Dil nga paneli</button>' +
              '<a class="btn btn-soft" href="index.html" target="_blank" rel="noopener">' + SB.icon('school', 'icon-sm') + 'Hap menyen per studentet</a>' +
            '</div>' +
          '</div>' +
          '<div class="card card-pad"><div class="panel-title"><h2>Gjendja e sistemit</h2></div>' +
            '<div class="stat-list">' +
              statLine('API', health.status) +
              statLine('Databaza', health.databaza) +
              statLine('Ora e shkolles', health.koha) +
              statLine('Zona orare', health.zonaOrare) +
            '</div>' +
            '<p class="note-box section-gap">' + SB.icon('info', 'icon-sm') +
            'Te dhenat e byfese ruhen ne PostgreSQL. Fjalakalimet ruhen te enkriptuar me PBKDF2, asnje here ne tekst te qarte.</p>' +
          '</div>' +
        '</div>' +
        passwordCard() +
      '</div>';

      var root = host.querySelector('[data-settings-root]');
      root.querySelector('#logoutNow').addEventListener('click', A.signOut);
      bindPasswordForm(root);
    }).catch(function (error) {
      A.handleError(error, host);
    });
  }

  function bindPasswordForm(root) {
    var form = root.querySelector('#passwordForm');

    form.addEventListener('submit', function (event) {
      event.preventDefault();

      var current = form.querySelector('#CurrentPassword').value;
      var next = form.querySelector('#NewPassword').value;
      var repeat = form.querySelector('#RepeatPassword').value;

      if (next !== repeat) {
        SB.toast('Fjalakalimi i ri dhe perseritja nuk perputhen', 'error');
        return;
      }

      var button = form.querySelector('#savePassword');
      SB.busy(button, true, 'Duke ruajtur');

      SB.api.post('api/auth/password', { currentPassword: current, newPassword: next }).then(function (result) {
        SB.busy(button, false);
        SB.applyErrors(form, null);
        form.reset();
        SB.toast(result.message || 'Fjalakalimi u perditesua', 'success');
      }).catch(function (error) {
        SB.busy(button, false);

        if (error.errors) {
          SB.applyErrors(form, error.errors);
        }

        SB.toast(error.message, 'error');
      });
    });
  }
})(window);
