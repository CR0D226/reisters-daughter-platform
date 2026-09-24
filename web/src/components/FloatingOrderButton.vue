<template>
  <a
    class="floating-order"
    href="https://order.toasttab.com/online/the-reister-s-daughter-202-main-street"
    target="_blank"
    rel="noopener noreferrer"
    aria-label="Order online through Toast"
  >
    <span class="order-dot"></span>

    <span class="order-text">
      <small>Hungry?</small>
      <strong>Order Online →</strong>
    </span>
  </a>
</template>

<style scoped>
.floating-order {
  position: fixed;
  right: 28px;
  bottom: 28px;
  z-index: 1000;

  display: flex;
  align-items: center;
  gap: 12px;

  padding: 14px 20px;
  border-radius: 999px;

  background: #1c1c1c;
  color: #ffffff;

  box-shadow: 0 8px 30px rgba(0, 0, 0, 0.2);

  transition:
    transform 0.2s ease,
    box-shadow 0.2s ease;
}

.floating-order:hover {
  transform: translateY(-3px);
  box-shadow: 0 12px 35px rgba(0, 0, 0, 0.28);
}

.order-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: #ffffff;
}

.order-text {
  display: flex;
  flex-direction: column;
  line-height: 1.15;
}

.order-text small {
  margin-bottom: 3px;
  font-size: 0.7rem;
  opacity: 0.7;
}

.order-text strong {
  font-size: 0.9rem;
}

@media (max-width: 600px) {
  .floating-order {
    right: 16px;
    bottom: 16px;
    padding: 13px 17px;
  }

  .order-text small {
    display: none;
  }
}
</style>