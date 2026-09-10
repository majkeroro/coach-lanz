/* api-client.js — plugs the pages into the ASP.NET Core backend.
   Overrides window.BS data methods with server-backed versions.
   - Reads:  live config is injected per-request as window.SERVER_CONFIG.
   - Slots/prices/bookings: REST (/api/...), server is source of truth,
     so every device sees the same availability.
   - If SERVER_CONFIG is absent (plain static hosting), this file does
     nothing and the localStorage demo engine keeps working. */
(function(){
  if(!window.BS || !window.SERVER_CONFIG) return;
  const Local = { ...window.BS };

  async function J(url, opt){
    const r = await fetch(url, { credentials: "same-origin",
      headers: { "Content-Type": "application/json" }, ...(opt||{}) });
    if (r.status === 204) return null;
    const data = await r.json().catch(() => ({}));
    if (!r.ok) throw new Error(data.error || ("Request failed (" + r.status + ")"));
    return data;
  }
  const OV_KEY = "bs_config_override_v1";
  const getOv = () => { try { return JSON.parse(localStorage.getItem(OV_KEY) || "{}"); } catch { return {}; } };

  // Sync reads from server-injected config (+ local preview overrides).
  window.BS.getConfig = () =>
    Local.deepMerge(JSON.parse(JSON.stringify(window.SERVER_CONFIG)), getOv());

  // Live availability from the DB (same shape as before).
  window.BS.generateSlots = (date, serviceId, staffId, excludeRef) => {
    const q = new URLSearchParams({ date, serviceId, staffId: staffId || "any" });
    if (excludeRef) q.set("excludeRef", excludeRef);
    return J("/api/availability/slots?" + q);
  };

  // Same math as the engine, but against the merged server config.
  window.BS.priceCalc = (serviceId, addonIds, couponCode) => {
    const cfg = window.BS.getConfig();
    const svc = cfg.services.find(s => s.id === serviceId);
    const base = svc ? svc.price : 0;
    const addons = (cfg.addons || []).filter(a => (addonIds || []).includes(a.id));
    const addTotal = addons.reduce((s, a) => s + a.price, 0);
    let total = base + addTotal, discount = 0;
    const code = String(couponCode || "").trim().toLowerCase();
    const cp = (cfg.coupons || []).find(c => String(c.code).toLowerCase() === code);
    if (cp && total > 0 && (cp.minTotal == null || total >= cp.minTotal)) {
      discount = cp.type === "percent" ? Math.round(total * cp.value / 100) : Math.min(cp.value, total);
      total = total - discount;
    }
    return { base, addTotal, discount, total, coupon: discount ? cp : null };
  };

  // Mutations → REST. Server validates + prices (409 = slot just taken).
  window.BS.createBooking = (d) => J("/api/bookings", { method: "POST", body: JSON.stringify({
    serviceId: d.serviceId, staffId: d.staffId, locationId: d.locationId,
    date: d.date, time: d.time, addons: d.addons || [], coupon: d.coupon || "",
    name: d.name, phone: d.phone, email: d.email || "", notes: d.notes || "", pay: d.pay || "Cash" }) });

  // Customer self-service (phone verified server-side).
  window.BS.apiPatch = (ref, patch) =>
    J("/api/bookings/" + encodeURIComponent(ref), { method: "PATCH", body: JSON.stringify(patch) });
  window.BS.findBookings = (q) =>
    J("/api/bookings/lookup?q=" + encodeURIComponent(q || ""));

  // Admin (requires login cookie).
  window.BS.adminBookings = (params) =>
    J("/api/admin/bookings?" + new URLSearchParams(params || {}));
  window.BS.adminStats = () => J("/api/admin/stats");
  window.BS.adminPatch = (ref, patch) =>
    J("/api/admin/bookings/" + encodeURIComponent(ref), { method: "PATCH", body: JSON.stringify(patch) });
  window.BS.adminDelete = (ref) =>
    J("/api/bookings/" + encodeURIComponent(ref), { method: "DELETE" });

  // Admin config edits → full-config PUT (server is master; local preview cleared).
  window.BS.saveOverride = async (patch) => {
    const merged = Local.deepMerge(window.BS.getConfig(), patch);
    const saved = await J("/api/admin/config", { method: "PUT", body: JSON.stringify(merged) });
    window.SERVER_CONFIG = saved;
    localStorage.removeItem(OV_KEY);
    return saved;
  };
  window.BS.clearOverride = async () => { localStorage.removeItem(OV_KEY); };

  // Legacy local-only helpers stay available but are no longer authoritative.
  window.BS.local = Local;
})();
