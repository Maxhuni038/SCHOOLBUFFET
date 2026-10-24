(function (global) {
  var SB = global.SB = global.SB || {};

  function ApiError(message, status, errors) {
    var error = new Error(message);
    error.name = 'ApiError';
    error.status = status;
    error.errors = errors || null;
    return error;
  }

  function describe(status, data) {
    if (data && typeof data.message === 'string' && data.message.length) {
      return data.message;
    }

    if (status === 401) {
      return 'Sesioni u perfundua. Hyresh perseri per te vazhduar.';
    }

    if (status === 403) {
      return 'Nuk ke te drejta per kete veprim.';
    }

    if (status === 404) {
      return 'Te dhenat e kerkuar nuk u gjeten.';
    }

    if (status === 429) {
      return 'Shume perpjekje radhazi. Prisni pak dhe provoni perseri.';
    }

    if (status >= 500) {
      return 'Serveri ra ne gabim. Provoni perseri per pak minuta.';
    }

    return 'Kerkesa deshtoi. Kontrollo lidhjen me internetin.';
  }

  async function request(path, options) {
    var settings = options || {};
    var init = {
      method: settings.method || 'GET',
      credentials: 'same-origin',
      headers: {}
    };

    if (settings.body !== undefined) {
      if (settings.body instanceof FormData) {
        init.body = settings.body;
      } else {
        init.headers['Content-Type'] = 'application/json';
        init.body = JSON.stringify(settings.body);
      }
    }

    var response;
    try {
      response = await fetch(path, init);
    } catch (networkError) {
      throw ApiError('Serveri i byfese nuk arrihet. Sigurohu qe Backend-i eshte ne pune.', 0, null);
    }

    var payload = null;
    var text = await response.text();

    if (text.length) {
      try {
        payload = JSON.parse(text);
      } catch (parseError) {
        payload = { message: text };
      }
    }

    if (!response.ok) {
      throw ApiError(describe(response.status, payload), response.status, payload && payload.errors);
    }

    return payload;
  }

  SB.api = {
    get: function (path) {
      return request(path);
    },
    post: function (path, body) {
      return request(path, { method: 'POST', body: body === undefined ? {} : body });
    },
    put: function (path, body) {
      return request(path, { method: 'PUT', body: body });
    },
    patch: function (path, body) {
      return request(path, { method: 'PATCH', body: body });
    },
    del: function (path) {
      return request(path, { method: 'DELETE' });
    },
    upload: function (path, formData) {
      return request(path, { method: 'POST', body: formData });
    }
  };

  SB.isAuthError = function (error) {
    return error && (error.status === 401 || error.status === 403);
  };
})(window);
