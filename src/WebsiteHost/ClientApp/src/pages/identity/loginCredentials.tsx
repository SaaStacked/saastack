import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { HttpStatusCode } from 'axios';
import { LoginCredentialsAction } from '../../actions/identity/loginCredentials.ts';
import { authenticate } from '../../api/websiteHost';
import { Input } from '../../components';
import Form from '../../components/Form.tsx';


export const LoginCredentialsPage: React.FC = () => {
  const [formData, setFormData] = useState({
    username: '',
    password: ''
  });

  const { state } = LoginCredentialsAction();

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError(null);

    try {
      const response = await authenticate({
        body: {
          password: formData.password,
          provider: 'credentials',
          username: formData.username
        }
      });

      if (response.status === HttpStatusCode.Ok) {
        window.location.assign('/');
      }
    } catch (err: any) {
      setError(err.response?.data?.detail || 'Login failed. Please check your credentials.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center">
      <div className="text-center">
        <h1>Sign In</h1>
        //TODO: specify the action to the form //TODO: form should handle the errors and have the error component
        //TODO: no need to specify value={formData.username}, should be taken from the action request //TODO: no need to
        specify onchange, should be done by the form //TODO: form shoudl do the Submit, and isLoading and handle error
        and validation
        <Form action={login}>
          <Input
            name="username"
            label="Username"
            autoComplete="username"
            required
            value={formData.username}
            onChange={handleInputChange}
          />
          <Input
            type="password"
            name="password"
            label="Password"
            autoComplete="current-password"
            required
            value={formData.password}
            onChange={handleInputChange}
          />
        </Form>
        <div className="">
          <p>
            <Link to="/" className="btn btn-secondary">
              Go Home
            </Link>
          </p>
          <p>
            Don't have an account? <Link to="/identity/register-credentials">Register here</Link>
          </p>
        </div>
      </div>
    </div>
  );
};
