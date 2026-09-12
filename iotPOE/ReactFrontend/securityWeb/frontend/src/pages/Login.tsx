//(rudderz243,2026)
//(Vincent,2016)
import React, { useState } from "react";
import parkitechlogo from "../assets/parkitechlogo.png";
import { useNavigate } from "react-router-dom";

interface LoginProps {
  onLogin?: (username: string, password: string) => void;
}

export const Login: React.FC<LoginProps> = ({ onLogin }) => {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
const navigate = useNavigate();
  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!username || !password) {
      return;
    }

    if (onLogin) {
      onLogin(username, password);
    }

    console.log("Login:", { username, password });
  };

  return (
    <main className="login-page">
      <div className="login-card">

        {/* Logo */}
        <div className="login-brand">
          <div className="login-brand-mark">
            <img src={parkitechlogo} alt="Parkitech" />
          </div>

          <div>
            <strong>Parkitech</strong>
            <span>Security Dashboard</span>
          </div>
        </div>

        {/* Heading */}
        <div className="login-heading">
          <h1>Log In</h1>
          <p>Sign in to access the parking dashboard</p>
        </div>

        <hr />

        {/* Form */}
        <form onSubmit={handleSubmit} className="login-form">

          {/* Username */}
          <div className="login-field">
            <label htmlFor="username">
              User Name
              {username && (
                <i
                  className="fa fa-check login-check"
                  aria-hidden="true"
                />
              )}
            </label>

            <input
              id="username"
              className="login-input"
              type="text"
              value={username}
              onChange={(event) => setUsername(event.target.value)}
              autoComplete="username"
            />
          </div>

          {/* Password */}
          <div className="login-field">
            <label htmlFor="password">
              Password
              {password && (
                <i
                  className="fa fa-check login-check"
                  aria-hidden="true"
                />
              )}
            </label>

            <input
              id="password"
              className="login-input"
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              autoComplete="current-password"
            />
          </div>

          <hr />

          {/* Login button */}
          <button
            className="login-button"
            type="button"
            onClick={()=> navigate("/dashboard")}
            disabled={!username || !password}
          >
            LOGIN
          </button>

        </form>

        <p className="login-footer">
          Parkitech Security System
        </p>

      </div>
    </main>
  );
};

export default Login;
{/* References
Thakekar,A.,2026.IoT-Admin Panel. (Version 2.0) [Source code] Available at: < https://codepen.io/adityathakekar/pen/MWObQoy > [Accessed 10 Septemeber 2026]. 
  */}