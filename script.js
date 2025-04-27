document.getElementById('loginForm').addEventListener('submit', function(event) {
    event.preventDefault(); // zatrzymuje domyślne wysyłanie formularza

    const username = document.getElementById('username').value.trim();
    const password = document.getElementById('password').value.trim();
    const errorMessage = document.getElementById('error-message');

    if (username === '' || password === '') {
        errorMessage.style.display = 'block'; // pokazujemy błąd
    } else {
        errorMessage.style.display = 'none';
        alert('Logowanie poprawne!');
    }
});
