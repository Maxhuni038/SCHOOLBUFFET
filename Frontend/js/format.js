(function (global) {
  var SB = global.SB = global.SB || {};

  var MONTHS = ['Janar', 'Shkurt', 'Mars', 'Prill', 'Maj', 'Qershor', 'Korrik', 'Gusht', 'Shtator', 'Tetor', 'Nentor', 'Dhjetor'];
  var WEEKDAYS = ['E diele', 'E hene', 'E Marte', 'E Merkure', 'E Enjte', 'E Premte', 'E Shtune'];

  var moneyFormat = new Intl.NumberFormat('de-DE', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  var intFormat = new Intl.NumberFormat('de-DE', { maximumFractionDigits: 0 });

  SB.currencyLabel = '€';

  SB.money = function (value) {
    return moneyFormat.format(Number(value) || 0) + ' ' + SB.currencyLabel;
  };

  SB.moneyStrict = function (value) {
    return moneyFormat.format(Number(value) || 0) + ' ' + SB.currencyLabel;
  };

  SB.number = function (value) {
    return intFormat.format(Number(value) || 0);
  };

  SB.utc = function (value) {
    if (!value) {
      return null;
    }

    var date = new Date(value);
    return isNaN(date.getTime()) ? null : date;
  };

  function parts(value) {
    var bits = String(value || '').slice(0, 10).split('-');
    return bits.length === 3 ? bits : null;
  }

  SB.plainDay = function (value) {
    var bits = parts(value);

    if (!bits) {
      return 'Pa date';
    }

    return Number(bits[2]) + ' ' + (MONTHS[Number(bits[1]) - 1] || bits[1]) + ' ' + bits[0];
  };

  SB.plainShort = function (value) {
    var bits = parts(value);

    if (!bits) {
      return '';
    }

    var short = {
      '01': 'Jan', '02': 'Shk', '03': 'Mar', '04': 'Pri', '05': 'Maj', '06': 'Qer',
      '07': 'Kor', '08': 'Gus', '09': 'Sht', '10': 'Tet', '11': 'Nen', '12': 'Dhj'
    };

    return Number(bits[2]) + ' ' + (short[bits[1]] || bits[1]);
  };

  SB.dayLabel = function (value) {
    var date = SB.utc(value);
    if (!date) {
      return 'Pa date';
    }

    return date.getDate() + ' ' + MONTHS[date.getMonth()] + ' ' + date.getFullYear();
  };

  SB.dayShort = function (value) {
    var date = SB.utc(value);
    if (!date) {
      return '';
    }

    return date.getDate() + ' ' + MONTHS[date.getMonth()].slice(0, 3);
  };

  SB.weekday = function (value) {
    var date = SB.utc(value);
    return date ? WEEKDAYS[date.getDay()] : '';
  };

  SB.time = function (value) {
    var date = SB.utc(value);
    if (!date) {
      return '';
    }

    return String(date.getHours()).padStart(2, '0') + ':' + String(date.getMinutes()).padStart(2, '0');
  };

  SB.dateTime = function (value) {
    return SB.dayShort(value) + ' · ' + SB.time(value);
  };

  SB.relative = function (value) {
    var date = SB.utc(value);
    if (!date) {
      return '';
    }

    var diff = Math.round((Date.now() - date.getTime()) / 1000);

    if (diff < 60) {
      return 'tani';
    }

    if (diff < 3600) {
      return 'para ' + Math.floor(diff / 60) + ' min';
    }

    if (diff < 86400) {
      return 'para ' + Math.floor(diff / 3600) + ' ore';
    }

    var days = Math.floor(diff / 86400);
    return days === 1 ? 'dje' : 'para ' + days + ' dite';
  };

  SB.todayKey = function () {
    var date = new Date();
    return [
      date.getFullYear(),
      String(date.getMonth() + 1).padStart(2, '0'),
      String(date.getDate()).padStart(2, '0')
    ].join('-');
  };

  SB.dayKey = function (offsetDays) {
    var date = new Date();
    date.setDate(date.getDate() + (offsetDays || 0));
    return [
      date.getFullYear(),
      String(date.getMonth() + 1).padStart(2, '0'),
      String(date.getDate()).padStart(2, '0')
    ].join('-');
  };

  SB.esc = function (value) {
    return String(value === null || value === undefined ? '' : value)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  };

  var STATUS_META = {
    NePritje: { label: 'Ne pritje', pill: 'pill-amber', icon: 'clock', color: '#f5a524' },
    DukeUPergatitur: { label: 'Duke u pergatitur', pill: 'pill-blue', icon: 'pot', color: '#4f7cff' },
    Gati: { label: 'Gati', pill: 'pill-green', icon: 'bell', color: '#16c79a' },
    Perfunduar: { label: 'Perfunduar', pill: 'pill-pink', icon: 'check-circle', color: '#8f7bff' }
  };

  SB.statusMeta = function (key) {
    return STATUS_META[key] || { label: key || 'Pa status', pill: 'pill', icon: 'clock', color: '#8b95ac' };
  };

  SB.statusFlow = ['NePritje', 'DukeUPergatitur', 'Gati', 'Perfunduar'];

  SB.pill = function (key) {
    var meta = SB.statusMeta(key);
    return '<span class="pill ' + meta.pill + '">' + SB.esc(meta.label) + '</span>';
  };
})(window);
