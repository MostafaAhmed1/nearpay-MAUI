import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:nearpay_flutter_sdk/nearpay.dart';
import 'package:permission_handler/permission_handler.dart';

void main() => runApp(const NearpayOneButtonApp());

class NearpayOneButtonApp extends StatelessWidget {
  const NearpayOneButtonApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'NearPay One-Button',
      theme: ThemeData(colorSchemeSeed: Colors.indigo),
      home: const NearpayOneButtonPage(),
    );
  }
}

class NearpayOneButtonPage extends StatefulWidget {
  const NearpayOneButtonPage({super.key});

  @override
  State<NearpayOneButtonPage> createState() => _NearpayOneButtonPageState();
}

class _NearpayOneButtonPageState extends State<NearpayOneButtonPage> {
  final _authValueController = TextEditingController();
  final _amountController = TextEditingController(text: '100');
  final _customerRefController = TextEditingController();
  final _finishTimeoutController = TextEditingController(text: '10');

  Environments _environment = Environments.sandbox;
  AuthenticationType _authType = AuthenticationType.login;

  bool _enableUi = true;
  bool _enableReversal = true;
  bool _enableUiDismiss = true;

  bool _busy = false;
  String _log = '';

  @override
  void dispose() {
    _authValueController.dispose();
    _amountController.dispose();
    _customerRefController.dispose();
    _finishTimeoutController.dispose();
    super.dispose();
  }

  void _appendLog(String title, Object? value) {
    final line = '[${DateTime.now().toIso8601String()}] $title\n$value\n';
    setState(() => _log = _log.isEmpty ? line : '$_log\n$line');
  }

  Map<String, dynamic> _normalizeResponse(dynamic response) {
    if (response is Map) {
      return response.map((k, v) => MapEntry(k.toString(), v));
    }
    if (response is String) {
      final decoded = jsonDecode(response);
      if (decoded is Map) {
        return decoded.map((k, v) => MapEntry(k.toString(), v));
      }
      return {'raw': response};
    }
    return {'raw': response?.toString()};
  }

  Future<void> _runOneButton() async {
    if (_busy) return;

    setState(() => _busy = true);
    try {
      final locationStatus = await Permission.location.request();
      _appendLog('Permission.location', locationStatus);
      if (!locationStatus.isGranted) {
        _appendLog('ERROR', 'Location permission is required');
        return;
      }

      final authValue = _authValueController.text.trim();
      if (_authType != AuthenticationType.login && authValue.isEmpty) {
        _appendLog(
          'ERROR',
          'Auth Value is required for the selected auth type',
        );
        return;
      }

      final amount = int.tryParse(_amountController.text.trim());
      if (amount == null || amount <= 0) {
        _appendLog('ERROR', 'Amount must be a positive integer (minor units)');
        return;
      }

      final finishTimeout = int.tryParse(_finishTimeoutController.text.trim());
      if (finishTimeout == null || finishTimeout <= 0) {
        _appendLog('ERROR', 'Finish timeout must be a positive integer');
        return;
      }

      final initData = <String, dynamic>{
        'authtype': _authType.value,
        'authvalue': _authType == AuthenticationType.login ? '' : authValue,
        'locale': Locale.localeDefault.value,
        'environment': _environment.value,
      };
      final initResponse = await Nearpay.initialize(initData);
      _appendLog('Initialize', initResponse);
      final initJson = _normalizeResponse(initResponse);
      if (initJson['status'] != 200) return;

      final setupResponse = await Nearpay.setup();
      _appendLog('Setup', setupResponse);
      final setupJson = _normalizeResponse(setupResponse);
      if (setupJson['status'] != 200) return;

      final purchaseData = <String, dynamic>{
        'amount': amount,
        'customer_reference_number': _customerRefController.text.trim(),
        'isEnableUI': _enableUi,
        'isEnableReversal': _enableReversal,
        'finishTimeout': finishTimeout,
        'isUiDismissible': _enableUiDismiss,
      };
      final purchaseResponse = await Nearpay.purchase(purchaseData);
      _appendLog('Purchase', purchaseResponse);
    } catch (e) {
      _appendLog('EXCEPTION', e);
    } finally {
      setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('NearPay One-Button (Android)')),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              InputDecorator(
                decoration: const InputDecoration(labelText: 'Environment'),
                child: DropdownButtonHideUnderline(
                  child: DropdownButton<Environments>(
                    value: _environment,
                    items: Environments.values
                        .map(
                          (e) =>
                              DropdownMenuItem(value: e, child: Text(e.value)),
                        )
                        .toList(),
                    onChanged: _busy
                        ? null
                        : (v) => setState(() => _environment = v!),
                    isExpanded: true,
                  ),
                ),
              ),
              const SizedBox(height: 12),
              InputDecorator(
                decoration: const InputDecoration(labelText: 'Auth Type'),
                child: DropdownButtonHideUnderline(
                  child: DropdownButton<AuthenticationType>(
                    value: _authType,
                    items: AuthenticationType.values
                        .map(
                          (e) =>
                              DropdownMenuItem(value: e, child: Text(e.value)),
                        )
                        .toList(),
                    onChanged: _busy
                        ? null
                        : (v) => setState(() => _authType = v!),
                    isExpanded: true,
                  ),
                ),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: _authValueController,
                enabled: !_busy,
                decoration: const InputDecoration(
                  labelText: 'Auth Value (Email/Mobile/JWT)',
                ),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: _amountController,
                enabled: !_busy,
                keyboardType: TextInputType.number,
                decoration: const InputDecoration(
                  labelText: 'Amount (minor units)',
                  hintText: '100 = 1.00',
                ),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: _finishTimeoutController,
                enabled: !_busy,
                keyboardType: TextInputType.number,
                decoration: const InputDecoration(
                  labelText: 'Finish Timeout (s)',
                ),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: _customerRefController,
                enabled: !_busy,
                decoration: const InputDecoration(
                  labelText: 'Customer Reference (optional)',
                ),
              ),
              const SizedBox(height: 12),
              SwitchListTile(
                value: _enableUi,
                onChanged: _busy ? null : (v) => setState(() => _enableUi = v),
                title: const Text('Enable UI'),
              ),
              SwitchListTile(
                value: _enableReversal,
                onChanged: _busy
                    ? null
                    : (v) => setState(() => _enableReversal = v),
                title: const Text('Enable Reversal'),
              ),
              SwitchListTile(
                value: _enableUiDismiss,
                onChanged: _busy
                    ? null
                    : (v) => setState(() => _enableUiDismiss = v),
                title: const Text('Enable UI Dismiss'),
              ),
              const SizedBox(height: 12),
              ElevatedButton(
                onPressed: _busy ? null : _runOneButton,
                child: Text(
                  _busy ? 'Running...' : 'Run: Initialize → Setup → Purchase',
                ),
              ),
              const SizedBox(height: 12),
              OutlinedButton(
                onPressed: _busy ? null : () => setState(() => _log = ''),
                child: const Text('Clear log'),
              ),
              const SizedBox(height: 12),
              SelectableText(
                _log.isEmpty ? '(no log yet)' : _log,
                style: const TextStyle(fontFamily: 'monospace'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
