(function (global) {
  var SB = global.SB = global.SB || {};
  var A = SB.A;

  var ICONS = ['sandwich', 'drink', 'food', 'dessert', 'fruit', 'coffee', 'milk', 'bread', 'salad', 'snack'];

  var GALLERY = [
    '/assets/img/products/sandwich-pule.svg',
    '/assets/img/products/sandwich-turshi.svg',
    '/assets/img/products/toste-pishit-banane.svg',
    '/assets/img/products/panini-perime.svg',
    '/assets/img/products/ushqim-ftohte-familjar.svg',
    '/assets/img/products/byrek-me-kos.svg',
    '/assets/img/products/snack-perimesh.svg',
    '/assets/img/products/uje-natyrale.svg',
    '/assets/img/products/leng-portokalli.svg',
    '/assets/img/products/leng-molle.svg',
    '/assets/img/products/jogurt-perpirje.svg',
    '/assets/img/products/keks-me-shokollate.svg',
    '/assets/img/products/muffin-me-arra.svg',
    '/assets/img/products/biskota-me-sjere.svg',
    '/assets/img/products/default-product.svg'
  ];

  var products = {
    list: [],
    categories: [],
    search: '',
    categoryId: '',
    inactive: 'true'
  };

  var categories = {
    list: []
  };

  function categoryById(id) {
    return products.categories.filter(function (item) {
      return item.id === Number(id);
    })[0] || null;
  }

  function productQuery() {
    var bits = ['includeInactive=' + products.inactive];

    if (products.search) {
      bits.push('search=' + encodeURIComponent(products.search));
    }

    if (products.categoryId) {
      bits.push('categoryId=' + encodeURIComponent(products.categoryId));
    }

    return bits.join('&');
  }

  function productRow(product) {
    return '<tr>' +
      '<td><div class="prod-cell"><img src="' + SB.esc(product.imagePath) + '" alt="' + SB.esc(product.name) + '" width="52" height="40">' +
      '<div><b>' + SB.esc(product.name) + '</b>' +
      '<p class="tiny muted">' + SB.esc(product.badge || 'Pa etikete') + (product.isRecommended ? ' · i rekomanduar' : '') + '</p></div></div></td>' +
      '<td>' + SB.esc(product.categoryName) + '</td>' +
      '<td class="num">' + SB.moneyStrict(product.price) + '</td>' +
      '<td class="num">' + SB.number(product.timesOrdered) + '</td>' +
      '<td>' + (product.isActive
        ? '<span class="pill pill-green">' + SB.icon('check', 'icon-sm') + 'Aktiv</span>'
        : '<span class="pill pill-red">' + SB.icon('close', 'icon-sm') + 'Jashte meny</span>') + '</td>' +
      '<td class="muted nowrap">' + SB.esc(SB.dayLabel(product.createdAtUtc)) + '</td>' +
      '<td><div class="row-actions">' + (A.canCatalog()
        ? '<button class="btn btn-ghost btn-sm" type="button" data-edit="' + product.id + '">' + SB.icon('settings', 'icon-sm') + 'Redakto</button>' +
          '<button class="btn btn-ghost btn-sm" type="button" data-drop="' + product.id + '">' + SB.icon('trash', 'icon-sm') + 'Fshij</button>'
        : '<span class="tiny muted">Vetem administratori</span>') +
      '</div></td>' +
    '</tr>';
  }

  function productToolbar() {
    return '<div class="filter-bar">' +
      '<label class="search"><span class="sr-only">Kerko produkt</span>' + SB.icon('search') +
      '<input class="input" id="productSearch" placeholder="Emri ose perberja" value="' + SB.esc(products.search) + '"></label>' +
      '<select class="select" id="productCategory" aria-label="Filtro sipas kategorise">' +
        '<option value="">Gjitha kategorite</option>' +
        products.categories.map(function (category) {
          return '<option value="' + category.id + '"' + (products.categoryId === String(category.id) ? ' selected' : '') + '>' +
            SB.esc(category.name) + '</option>';
        }).join('') +
      '</select>' +
      '<select class="select" id="productState" aria-label="Cilesia e produkteve">' +
        '<option value="true"' + (products.inactive === 'true' ? ' selected' : '') + '>Aktiv dhe joaktiv</option>' +
        '<option value="false"' + (products.inactive === 'false' ? ' selected' : '') + '>Vetem aktiv</option>' +
      '</select>' +
    '</div>';
  }

  function paintProducts(host) {
    var active = products.list.filter(function (item) {
      return item.isActive;
    }).length;

    host.innerHTML = '<div class="stack" data-products-root>' + productToolbar() +
      (products.list.length
        ? '<div class="table-wrap"><table class="data"><thead><tr><th>Produkti</th><th>Kategoria</th>' +
          '<th class="num">Cmimi</th><th class="num">Porosi</th><th>Statusi</th><th>Rregjistrimi</th>' +
          '<th class="num">Veprime</th></tr></thead>' +
          '<tbody>' + products.list.map(productRow).join('') + '</tbody></table></div>'
        : A.empty('sandwich', 'Asnje produkt ne kete katalog. Shto produktin e pare me butonin produkt i ri.')) +
      '<p class="tiny muted">' + products.list.length + ' rreshta · ' + active + ' aktiv ne meny</p>' +
    '</div>';

    var root = host.querySelector('[data-products-root]');

    root.addEventListener('click', function (event) {
      var edit = event.target.closest('[data-edit]');

      if (edit) {
        openProductForm(findProduct(Number(edit.getAttribute('data-edit'))), host);
        return;
      }

      var drop = event.target.closest('[data-drop]');

      if (drop) {
        deleteProduct(findProduct(Number(drop.getAttribute('data-drop'))), host);
      }
    });

    root.querySelector('#productSearch').addEventListener('input', SB.debounce(function () {
      products.search = root.querySelector('#productSearch').value.trim();
      loadProducts(host);
    }, 320));

    root.querySelector('#productCategory').addEventListener('change', function () {
      products.categoryId = root.querySelector('#productCategory').value;
      loadProducts(host);
    });

    root.querySelector('#productState').addEventListener('change', function () {
      products.inactive = root.querySelector('#productState').value;
      loadProducts(host);
    });
  }

  function findProduct(id) {
    return products.list.filter(function (item) {
      return item.id === id;
    })[0] || null;
  }

  function loadProducts(host) {
    return SB.api.get('api/admin/products?' + productQuery()).then(function (list) {
      products.list = list;
      A.setSub(SB.number(list.length) + ' produkte ne katalog');
      paintProducts(host);
    }).catch(function (error) {
      A.handleError(error, host);
    });
  }

  function galleryHtml(selected) {
    var extra = GALLERY.concat([selected]).filter(function (path, index, all) {
      return path && path.indexOf('/assets/img/') === 0 && all.indexOf(path) === index;
    });

    return '<div class="gallery">' + extra.map(function (path) {
      return '<button type="button" data-pick="' + SB.esc(path) + '"' +
        (path === selected ? ' class="on"' : '') + '><img src="' + SB.esc(path) + '" alt="Foto e produktit" loading="lazy"></button>';
    }).join('') + '</div>';
  }

  function productForm(product) {
    var current = product || {
      categoryId: products.categories.length ? products.categories[0].id : 0,
      name: '',
      description: '',
      price: 0,
      imagePath: '/assets/img/products/default-product.svg',
      badge: '',
      isRecommended: false,
      sortOrder: 0,
      isActive: true,
      trackStock: false,
      stockQuantity: 0,
      lowStockThreshold: 10
    };

    return '<div class="split">' +
      '<div class="field"><label for="Name">Emri i produktit</label>' +
        '<input class="input" id="Name" name="Name" maxlength="80" value="' + SB.esc(current.name) + '" required>' +
        '<span class="field-error"></span></div>' +
      '<div class="field"><label for="CategoryId">Kategoria</label>' +
        '<select class="select" id="CategoryId" name="CategoryId">' +
          products.categories.map(function (category) {
            return '<option value="' + category.id + '"' + (Number(category.id) === Number(current.categoryId) ? ' selected' : '') + '>' +
              SB.esc(category.name) + '</option>';
          }).join('') +
        '</select><span class="field-error"></span></div>' +
    '</div>' +
    '<div class="field"><label for="Description">Pershkrimi</label>' +
      '<textarea class="textarea" id="Description" name="Description" maxlength="240">' + SB.esc(current.description) + '</textarea>' +
      '<span class="field-error"></span></div>' +
    '<div class="split">' +
      '<div class="field"><label for="Price">Cmimi ne euro</label>' +
        '<input class="input" id="Price" name="Price" type="number" min="0.2" max="500" step="0.05" value="' + SB.esc(current.price) + '">' +
        '<span class="field-error"></span></div>' +
      '<div class="field"><label for="Badge">Etiketa ne foto</label>' +
        '<input class="input" id="Badge" name="Badge" maxlength="24" placeholder="P.sh. Me i kerkuari" value="' + SB.esc(current.badge) + '">' +
        '<span class="field-error"></span></div>' +
    '</div>' +
    '<div class="split">' +
      '<div class="field"><label for="SortOrder">Renditja</label>' +
        '<input class="input" id="SortOrder" name="SortOrder" type="number" min="0" max="999" value="' + SB.esc(current.sortOrder) + '">' +
        '<span class="field-error"></span></div>' +
      '<div class="field"><label>Pa pamje ne meny</label>' +
        '<label class="toggle"><input type="checkbox" id="IsActive" name="IsActive"' + (current.isActive ? ' checked' : '') + '>' +
        '<i></i><span>Aktiv</span></label>' +
        '<label class="toggle"><input type="checkbox" id="IsRecommended" name="IsRecommended"' +
        (current.isRecommended ? ' checked' : '') + '><i></i><span>I rekomanduar</span></label></div>' +
    '</div>' +
    '<div class="split">' +
      '<div class="field"><label>Gjurmimi i sases</label>' +
        '<label class="toggle"><input type="checkbox" id="TrackStock" name="TrackStock"' + (current.trackStock ? ' checked' : '') + '>' +
        '<i></i><span>Gjurmo sasin</span></label>' +
        '<span class="tiny muted">Kur porosia krijohet, sasia zvogelohet automatikisht.</span></div>' +
      '<div class="field"><label for="LowStockThreshold">Praggi i ulete</label>' +
        '<input class="input" id="LowStockThreshold" name="LowStockThreshold" type="number" min="0" max="9999" value="' + SB.esc(current.lowStockThreshold) + '">' +
        '<span class="field-error"></span></div>' +
    '</div>' +
    '<div class="field"><label for="StockQuantity">Sasia ne dorze (copeke)</label>' +
      '<input class="input" id="StockQuantity" name="StockQuantity" type="number" min="0" max="99999" value="' + SB.esc(current.stockQuantity) + '">' +
      '<span class="field-error"></span></div>' +
    '<div class="field"><label>Fotoja e produktit</label>' +
      '<div class="preview"><img id="ImagePreview" src="' + SB.esc(current.imagePath) + '" alt="Parakalimi i fotos"></div>' +
      '<input type="hidden" id="ImagePath" name="ImagePath" value="' + SB.esc(current.imagePath) + '">' +
      '<div class="row wrap mt">' +
        '<input type="file" class="input" id="ImageFile" accept="image/png,image/jpeg,image/webp,image/svg+xml">' +
        '<button class="btn btn-soft btn-sm" type="button" id="UploadBtn">' + SB.icon('upload', 'icon-sm') + 'Ngarko foton</button>' +
      '</div>' +
      '<span class="tiny muted">Formatet: JPG, PNG, WEBP, SVG · maksimum 2 MB. Foto e ngarkuar ruhet ne server.</span>' +
      '<span class="field-error"></span></div>' +
      '<div class="field"><label>Galeria e fotoset</label>' + galleryHtml(current.imagePath) + '</div>';
  }

  function setImage(root, path) {
    var input = root.querySelector('#ImagePath');
    var preview = root.querySelector('#ImagePreview');

    input.value = path;
    preview.setAttribute('src', path);

    root.querySelectorAll('[data-pick]').forEach(function (button) {
      button.classList.toggle('on', button.getAttribute('data-pick') === path);
    });
  }

  function openProductForm(product, host) {
    var handle = SB.modal({
      title: product ? 'Redakto ' + product.name : 'Produkt i ri',
      subtitle: product ? 'Ndryshimet ruhen ne databaze menjehere' : 'Ploteso te dhenat per t\'e shtuar ne meny',
      html: productForm(product),
      actions: [
        { id: 'cancel', label: 'Anulo', className: 'btn-ghost' },
        { id: 'save', label: product ? 'Ruaj ndryshimet' : 'Shto produktin' }
      ],
      onAction: function (id, modal) {
        if (id === 'cancel') {
          modal.close();
          return;
        }

        saveProduct(modal, product, host);
      }
    });

    var root = handle.root;

    root.addEventListener('click', function (event) {
      var pick = event.target.closest('[data-pick]');

      if (pick) {
        setImage(root, pick.getAttribute('data-pick'));
      }
    });

    root.querySelector('#UploadBtn').addEventListener('click', function () {
      var file = root.querySelector('#ImageFile');

      if (!file.files || !file.files.length) {
        SB.toast('Zgjidh nje foto nga pajisja per ta ngarkuar', 'error');
        return;
      }

      var button = root.querySelector('#UploadBtn');
      var data = new FormData();
      data.append('file', file.files[0]);
      SB.busy(button, true, 'Duke ngarkuar');

      SB.api.upload('api/admin/uploads/image', data).then(function (result) {
        SB.busy(button, false);
        setImage(root, result.path);
        SB.toast('Fotoja u ngarkua ne server', 'success');
      }).catch(function (error) {
        SB.busy(button, false);
        SB.toast(error.message, 'error');
      });
    });
  }

  function saveProduct(handle, product, host) {
    var root = handle.root;
    var button = root.querySelector('[data-action="save"]');
    var payload = {
      categoryId: Number(root.querySelector('#CategoryId').value),
      name: root.querySelector('#Name').value.trim(),
      description: root.querySelector('#Description').value.trim(),
      price: Number(root.querySelector('#Price').value),
      imagePath: root.querySelector('#ImagePath').value,
      badge: root.querySelector('#Badge').value.trim(),
      isRecommended: root.querySelector('#IsRecommended').checked,
      sortOrder: Number(root.querySelector('#SortOrder').value) || 0,
      isActive: root.querySelector('#IsActive').checked,
      trackStock: root.querySelector('#TrackStock').checked,
      stockQuantity: Number(root.querySelector('#StockQuantity').value) || 0,
      lowStockThreshold: Number(root.querySelector('#LowStockThreshold').value) || 0
    };

    SB.busy(button, true, 'Duke ruajtur');

    var request = product
      ? SB.api.put('api/admin/products/' + product.id, payload)
      : SB.api.post('api/admin/products', payload);

    request.then(function () {
      handle.close();
      SB.toast(product ? 'Produkti u perditesua' : 'Produkti u shtua ne meny', 'success');
      return loadProducts(host);
    }).catch(function (error) {
      SB.busy(button, false);

      if (error.errors) {
        SB.applyErrors(root, error.errors);
      }

      SB.toast(error.message, 'error');
    });
  }

  function deleteProduct(product, host) {
    if (!product) {
      return;
    }

    SB.confirm({
      title: 'Fshij ' + product.name,
      text: 'Produkti hiqet nga menyja. Porosite e vjetra e ruajne emrin dhe cmimin.',
      okLabel: 'Po, fshije',
      danger: true
    }).then(function (yes) {
      if (!yes) {
        return;
      }

      SB.api.del('api/admin/products/' + product.id).then(function (result) {
        SB.toast(result.message || 'Produkti u fshi', 'success');
        return loadProducts(host);
      }).catch(function (error) {
        SB.toast(error.message, 'error');
      });
    });
  }

  function categoryRow(category) {
    return '<tr>' +
      '<td><div class="prod-cell"><span class="dot-swatch" data-swatch="' + SB.esc(category.accentColor) + '">' +
        SB.icon(category.icon, 'icon-sm') + '</span><div><b>' + SB.esc(category.name) + '</b>' +
        '<p class="tiny muted">' + SB.esc(category.description) + '</p></div></div></td>' +
      '<td class="num">' + SB.number(category.productCount) + '</td>' +
      '<td class="num">' + category.sortOrder + '</td>' +
      '<td>' + (category.isActive
        ? '<span class="pill pill-green">' + SB.icon('check', 'icon-sm') + 'Aktiv</span>'
        : '<span class="pill pill-red">' + SB.icon('close', 'icon-sm') + 'Fshehur</span>') + '</td>' +
      '<td class="muted nowrap">' + SB.esc(SB.dayLabel(category.createdAtUtc)) + '</td>' +
      '<td><div class="row-actions">' + (A.canCatalog()
        ? '<button class="btn btn-ghost btn-sm" type="button" data-edit="' + category.id + '">' + SB.icon('settings', 'icon-sm') + 'Redakto</button>' +
          '<button class="btn btn-ghost btn-sm" type="button" data-drop="' + category.id + '">' + SB.icon('trash', 'icon-sm') + 'Fshij</button>'
        : '<span class="tiny muted">Vetem administratori</span>') +
      '</div></td>' +
    '</tr>';
  }

  function paintCategories(host) {
    host.innerHTML = '<div class="stack" data-categories-root>' +
      (categories.list.length
        ? '<div class="table-wrap"><table class="data"><thead><tr><th>Kategoria</th><th class="num">Produkte</th>' +
          '<th class="num">Renditja</th><th>Statusi</th><th>Rregjistrimi</th>' +
          '<th class="num">Veprime</th></tr></thead><tbody>' +
          categories.list.map(categoryRow).join('') + '</tbody></table></div>'
        : A.empty('tag', 'Katalogu nuk ka kategori. Shto kategorine e pare per te organizuar menyne.')) +
      '<p class="note-box">' + SB.icon('info', 'icon-sm') +
      'Nje kategori nuk mund te fshijet nese ka produkte. Zhvendos produktet se pari.</p>' +
    '</div>';

    var root = host.querySelector('[data-categories-root]');

    root.querySelectorAll('[data-swatch]').forEach(function (node) {
      node.style.background = node.getAttribute('data-swatch');
    });

    root.addEventListener('click', function (event) {
      var edit = event.target.closest('[data-edit]');

      if (edit) {
        openCategoryForm(findCategory(Number(edit.getAttribute('data-edit'))), host);
        return;
      }

      var drop = event.target.closest('[data-drop]');

      if (drop) {
        deleteCategory(findCategory(Number(drop.getAttribute('data-drop'))), host);
      }
    });
  }

  function findCategory(id) {
    return categories.list.filter(function (item) {
      return item.id === id;
    })[0] || null;
  }

  function loadCategories(host) {
    return SB.api.get('api/admin/categories?includeInactive=true').then(function (list) {
      categories.list = list;
      products.categories = list.filter(function (item) {
        return item.isActive;
      });
      A.setSub(SB.number(list.length) + ' kategori ne meny');
      paintCategories(host);
    }).catch(function (error) {
      A.handleError(error, host);
    });
  }

  function categoryForm(category) {
    var current = category || {
      name: '',
      icon: 'snack',
      accentColor: '#6c5ce7',
      description: '',
      sortOrder: categories.list.length + 1,
      isActive: true
    };

    return '<div class="split">' +
      '<div class="field"><label for="CatName">Emri i kategorise</label>' +
        '<input class="input" id="CatName" name="Name" maxlength="40" value="' + SB.esc(current.name) + '" required>' +
        '<span class="field-error"></span></div>' +
      '<div class="field"><label for="CatIcon">Ikona</label>' +
        '<select class="select" id="CatIcon" name="Icon">' + ICONS.map(function (icon) {
          return '<option value="' + icon + '"' + (current.icon === icon ? ' selected' : '') + '>' +
            icon.charAt(0).toUpperCase() + icon.slice(1) + '</option>';
        }).join('') + '</select><span class="field-error"></span></div>' +
    '</div>' +
    '<div class="field"><label for="CatDescription">Pershkrimi</label>' +
      '<textarea class="textarea" id="CatDescription" name="Description" maxlength="160">' + SB.esc(current.description) + '</textarea>' +
      '<span class="field-error"></span></div>' +
    '<div class="split">' +
      '<div class="field"><label for="CatColor">Ngjyra</label>' +
        '<div class="row"><input class="input grow" id="CatColor" name="AccentColor" maxlength="7" value="' + SB.esc(current.accentColor) + '">' +
        '<input type="color" id="CatColorPick" class="color-well" aria-label="Zgjidh ngjyren"></div>' +
        '<span class="field-error"></span></div>' +
      '<div class="field"><label for="CatSort">Renditja</label>' +
        '<input class="input" id="CatSort" name="SortOrder" type="number" min="1" max="99" value="' + SB.esc(current.sortOrder) + '">' +
        '<span class="field-error"></span></div>' +
    '</div>' +
    '<label class="toggle"><input type="checkbox" id="CatActive" name="IsActive"' + (current.isActive ? ' checked' : '') + '>' +
      '<i></i><span>Kategoria shfaqet ne menyen e studenteve</span></label>';
  }

  function openCategoryForm(category, host) {
    var handle = SB.modal({
      title: category ? 'Redakto ' + category.name : 'Kategori e re',
      html: categoryForm(category),
      actions: [
        { id: 'cancel', label: 'Anulo', className: 'btn-ghost' },
        { id: 'save', label: category ? 'Ruaj ndryshimet' : 'Shto kategorine' }
      ],
      onAction: function (id, modal) {
        if (id === 'cancel') {
          modal.close();
          return;
        }

        saveCategory(modal, category, host);
      }
    });

    var color = handle.root.querySelector('#CatColorPick');
    color.value = /^[#][0-9a-fA-F]{6}$/.test(category ? category.accentColor : '') ? category.accentColor : '#6c5ce7';

    color.addEventListener('input', function () {
      handle.root.querySelector('#CatColor').value = color.value;
    });
  }

  function saveCategory(handle, category, host) {
    var root = handle.root;
    var button = root.querySelector('[data-action="save"]');
    var payload = {
      name: root.querySelector('#CatName').value.trim(),
      icon: root.querySelector('#CatIcon').value,
      accentColor: root.querySelector('#CatColor').value.trim(),
      description: root.querySelector('#CatDescription').value.trim(),
      sortOrder: Number(root.querySelector('#CatSort').value) || 1,
      isActive: root.querySelector('#CatActive').checked
    };

    SB.busy(button, true, 'Duke ruajtur');

    var request = category
      ? SB.api.put('api/admin/categories/' + category.id, payload)
      : SB.api.post('api/admin/categories', payload);

    request.then(function () {
      handle.close();
      SB.toast(category ? 'Kategoria u perditesua' : 'Kategoria u shtua', 'success');
      return loadCategories(host);
    }).catch(function (error) {
      SB.busy(button, false);

      if (error.errors) {
        SB.applyErrors(root, error.errors);
      }

      SB.toast(error.message, 'error');
    });
  }

  function deleteCategory(category, host) {
    if (!category) {
      return;
    }

    if (category.productCount > 0) {
      SB.toast('Kategoria ' + category.name + ' ka ' + category.productCount +
        ' produkte. Zhvendos produktet para se ta fshije.', 'error');
      return;
    }

    SB.confirm({
      title: 'Fshij kategorine ' + category.name,
      text: 'Kategoria hiqet nga menyja e studenteve.',
      okLabel: 'Po, fshije',
      danger: true
    }).then(function (yes) {
      if (!yes) {
        return;
      }

      SB.api.del('api/admin/categories/' + category.id).then(function (result) {
        SB.toast(result.message || 'Kategoria u fshi', 'success');
        return loadCategories(host);
      }).catch(function (error) {
        SB.toast(error.message, 'error');
      });
    });
  }

  A.views.products = {
    title: 'Produktet',
    skeleton: 3,
    render: function (host) {
      A.setActions([
        A.canCatalog() ? {
          id: 'new',
          label: 'Produkt i ri',
          icon: 'plus',
          onClick: function () {
            openProductForm(null, host);
          }
        } : null,
        {
          id: 'reload',
          label: 'Rifresko',
          className: 'btn-ghost',
          icon: 'refresh',
          onClick: function () {
            loadProducts(host);
          }
        }
      ]);

      A.defineReload(function () {
        loadProducts(host);
      });

      SB.api.get('api/admin/categories?includeInactive=false').then(function (list) {
        products.categories = list;
        return loadProducts(host);
      }).catch(function (error) {
        A.handleError(error, host);
      });
    }
  };

  A.views.categories = {
    title: 'Kategorite',
    skeleton: 2,
    render: function (host) {
      A.setActions([
        A.canCatalog() ? {
          id: 'new',
          label: 'Kategori e re',
          icon: 'plus',
          onClick: function () {
            openCategoryForm(null, host);
          }
        } : null,
        {
          id: 'reload',
          label: 'Rifresko',
          className: 'btn-ghost',
          icon: 'refresh',
          onClick: function () {
            loadCategories(host);
          }
        }
      ]);

      A.defineReload(function () {
        loadCategories(host);
      });

      loadCategories(host);
    }
  };
})(window);
