// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// 日期選擇器（v12）：系統內建日曆的大小由 iOS 決定、無法放大，改用 flatpickr 並在 site.css 放大，方便在 iPad 上用手指點選
(function () {
    if (typeof flatpickr === 'undefined') {
        return;
    }

    var locale = Object.assign({}, flatpickr.l10ns.zh_tw, { firstDayOfWeek: 1 });

    // 選填欄位（例如查詢條件）要能清空；flatpickr 的欄位為唯讀、無法直接刪字，因此在日曆下方加「清除」
    function addClearButton(fp) {
        var footer = document.createElement('div');
        footer.className = 'flatpickr-footer';
        var button = document.createElement('button');
        button.type = 'button';
        button.className = 'btn btn-outline-secondary btn-sm';
        button.textContent = '清除';
        button.addEventListener('click', function () {
            fp.clear();
            fp.close();
        });
        footer.appendChild(button);
        fp.calendarContainer.appendChild(footer);
    }

    document.querySelectorAll('input[type="date"], input[type="datetime-local"]').forEach(function (input) {
        var withTime = input.type === 'datetime-local';
        // 改為文字欄位，避免瀏覽器同時跳出系統日曆；送出的值格式與原本相同，後端不需修改
        input.type = 'text';
        if (withTime) {
            // 伺服器輸出的 datetime-local 值帶秒數（2026-10-10T19:00:00.000），只取到分鐘才能依 dateFormat 解析
            input.value = input.value.substring(0, 16);
        }

        flatpickr(input, {
            locale: locale,
            disableMobile: true,   // 預設在手機／平板改用系統日曆，那樣就無法放大
            enableTime: withTime,
            time_24hr: true,
            dateFormat: withTime ? 'Y-m-d\\TH:i' : 'Y-m-d',
            // 日期時間另以易讀格式顯示（不含 T），實際送出的仍是上面的 dateFormat
            altInput: withTime,
            altFormat: 'Y-m-d H:i',
            onReady: function (selectedDates, dateStr, fp) {
                if (!input.required && !input.hasAttribute('data-val-required')) {
                    addClearButton(fp);
                }
            }
        });
    });
})();
